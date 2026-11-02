using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace PlotboekGenerator.test.Pages;

/// <summary>
/// The admin screen behind Admin → Starttijden &amp; blokken. It is the only way to add a slot or retire
/// one, so these specs pin what it may and may not do to the reference data every stored plot points at.
/// </summary>
public sealed class AdminModel_Starttijden_specs
{
    private TestDocumentStore _documentStore;
    private AdminModel _sut;

    [SetUp]
    public void SetUp()
    {
        _documentStore = new TestDocumentStore();
        _sut = CreateModel(_documentStore);
    }

    [Test]
    public async Task Creates_a_slot_with_the_key_derived_from_its_time()
    {
        SeedZaterdagSlots();
        FillForm("13:30: Eindscène", laatsteDag: true, vensterLaatsteDag: "13:30 - 15:30");

        await _sut.HandleSaveStarttijd();

        var stored = _documentStore.Load<StarttijdConfig>(StarttijdIds.Tijd1330);

        stored.Should().NotBeNull();
        stored.DisplayName.Should().Be("13:30: Eindscène");
        stored.IsAvailableOnLaatsteDag.Should().BeTrue();
        stored.IsAvailableOnEersteDag.Should().BeFalse();
        stored.BlokVensterLaatsteDag.Should().Be("13:30 - 15:30");
        stored.IsNone.Should().BeFalse();
        _sut.ErrorMessage.Should().BeNull();
        _sut.SuccessMessage.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Gives_a_new_slot_the_next_free_enum_value()
    {
        SeedZaterdagSlots();
        FillForm("11:30", laatsteDag: true);

        await _sut.HandleSaveStarttijd();

        _documentStore.Load<StarttijdConfig>(StarttijdIds.Tijd1130).EnumValue.Should().Be(
            4,
            "the highest seeded value is 3 and the number only has to be unique — a new slot has no enum member");
    }

    [Test]
    public async Task Slides_a_new_slot_into_place_on_its_time()
    {
        SeedZaterdagSlots();
        FillForm("11:30", laatsteDag: true, vensterLaatsteDag: "11:30 - 13:30");

        await _sut.HandleSaveStarttijd();

        _documentStore.GetAll<StarttijdConfig>()
            .OrderBy(s => s.Order)
            .Select(s => (s.Id, s.Order))
            .Should().Equal(
                (StarttijdIds.None, 0),
                (StarttijdIds.Tijd1030, 1),
                (StarttijdIds.Tijd1130, 2),
                (StarttijdIds.Tijd1230, 3),
                (StarttijdIds.Tijd1430, 4));
    }

    [Test]
    public async Task Refuses_a_second_slot_on_the_same_time()
    {
        SeedZaterdagSlots();
        FillForm("10:30: Ochtendblok", middelDagen: true);

        await _sut.HandleSaveStarttijd();

        _sut.ErrorMessage.Should().Contain(StarttijdIds.Tijd1030);
        _sut.SuccessMessage.Should().BeNull();
        _documentStore.Load<StarttijdConfig>(StarttijdIds.Tijd1030).DisplayName.Should().Be(
            "10:30",
            "a name that derives an existing key must not overwrite that slot");
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task Refuses_an_empty_name(string naam)
    {
        SeedZaterdagSlots();
        FillForm(naam, middelDagen: true);

        await _sut.HandleSaveStarttijd();

        _sut.ErrorMessage.Should().NotBeNullOrEmpty();
        _documentStore.GetAll<StarttijdConfig>().Should().HaveCount(4);
    }

    [TestCase("Ochtend")]
    [TestCase("Blok om 11:30")]
    [TestCase("24:30")]
    public async Task Refuses_a_name_that_does_not_start_with_a_time(string naam)
    {
        SeedZaterdagSlots();
        FillForm(naam, middelDagen: true);

        await _sut.HandleSaveStarttijd();

        _sut.ErrorMessage.Should().Contain("tijd");
        _documentStore.GetAll<StarttijdConfig>().Should().HaveCount(4);
    }

    [Test]
    public async Task Keeps_the_key_when_a_slot_is_renamed()
    {
        SeedZaterdagSlots();
        FillForm("10:30: Ochtendblok", middelDagen: true, laatsteDag: true, vensterMiddelDagen: "9:30 - 11:30");
        _sut.EditStarttijdId = StarttijdIds.Tijd1030;

        await _sut.HandleSaveStarttijd();

        var stored = _documentStore.Load<StarttijdConfig>(StarttijdIds.Tijd1030);

        stored.DisplayName.Should().Be("10:30: Ochtendblok");
        stored.EnumValue.Should().Be(1, "the key and the enum value are what stored plots resolve through");
        _documentStore.GetAll<StarttijdConfig>().Should().HaveCount(4);
    }

    [TestCase("12:30")]
    [TestCase("12:30 ")]
    public async Task Refuses_a_rename_onto_another_slots_name(string naam)
    {
        SeedZaterdagSlots();
        FillForm(naam, middelDagen: true);
        _sut.EditStarttijdId = StarttijdIds.Tijd1030;

        await _sut.HandleSaveStarttijd();

        _sut.ErrorMessage.Should().Contain("12:30");
        _documentStore.Load<StarttijdConfig>(StarttijdIds.Tijd1030).DisplayName.Should().Be(
            "10:30",
            "two slots under one name are indistinguishable in every dropdown and time schedule");
    }

    [Test]
    public async Task Retires_a_slot_when_every_box_is_unchecked()
    {
        SeedZaterdagSlots();
        FillForm("14:30");
        _sut.EditStarttijdId = StarttijdIds.Tijd1430;

        await _sut.HandleSaveStarttijd();

        var stored = _documentStore.Load<StarttijdConfig>(StarttijdIds.Tijd1430);

        stored.IsAvailableOnEersteDag.Should().BeFalse();
        stored.IsAvailableOnMiddelDagen.Should().BeFalse();
        stored.IsAvailableOnLaatsteDag.Should().BeFalse();
        _sut.ErrorMessage.Should().BeNull();
        _documentStore.Load<StarttijdConfig>(StarttijdIds.Tijd1430).Should().NotBeNull(
            "retiring a slot may never delete it — old plots still point at the key");
    }

    [Test]
    public async Task Refuses_to_edit_the_none_sentinel()
    {
        SeedZaterdagSlots();
        FillForm("00:30");
        _sut.EditStarttijdId = StarttijdIds.None;

        await _sut.HandleSaveStarttijd();

        _sut.ErrorMessage.Should().NotBeNullOrEmpty();
        _documentStore.Load<StarttijdConfig>(StarttijdIds.None).DisplayName.Should().Be("-");
    }

    [Test]
    public async Task Refuses_to_edit_a_slot_that_is_gone()
    {
        SeedZaterdagSlots();
        FillForm("11:30", laatsteDag: true);
        _sut.EditStarttijdId = StarttijdIds.Tijd1130;

        await _sut.HandleSaveStarttijd();

        _sut.ErrorMessage.Should().NotBeNullOrEmpty();
        _documentStore.Load<StarttijdConfig>(StarttijdIds.Tijd1130).Should().BeNull(
            "an edit of a vanished row must not quietly recreate it under that key");
    }

    [Test]
    public async Task Trims_a_window_and_stores_a_blank_one_as_empty()
    {
        SeedZaterdagSlots();
        FillForm("11:30", laatsteDag: true, vensterLaatsteDag: "  11:30 - 13:30  ");
        _sut.EditStarttijdBlokVensterMiddelDagen = "   ";

        await _sut.HandleSaveStarttijd();

        var stored = _documentStore.Load<StarttijdConfig>(StarttijdIds.Tijd1130);

        stored.BlokVensterLaatsteDag.Should().Be("11:30 - 13:30");
        stored.BlokVensterMiddelDagen.Should().BeEmpty();
        stored.BlokVensterEersteDag.Should().BeEmpty();
    }

    [Test]
    public async Task Clears_the_form_after_a_save()
    {
        SeedZaterdagSlots();
        FillForm("11:30", laatsteDag: true, vensterLaatsteDag: "11:30 - 13:30");

        await _sut.HandleSaveStarttijd();

        _sut.EditStarttijdId.Should().BeNull();
        _sut.EditStarttijdNaam.Should().BeNull();
        _sut.EditStarttijdLaatsteDag.Should().BeFalse();
        _sut.EditStarttijdBlokVensterLaatsteDag.Should().BeNull();
    }

    [Test]
    public void Overzicht_numbers_the_slots_that_have_a_window()
    {
        _sut.Starttijden =
        [
            Slot(StarttijdIds.Tijd1130, "11:30", 9, 1, laatsteDag: true, vensterLaatsteDag: "11:30 - 13:30"),
            Slot(StarttijdIds.Tijd1330, "13:30: Eindscène", 10, 2, laatsteDag: true, vensterLaatsteDag: "13:30 - 15:30"),
        ];

        _sut.BuildStarttijdOverzicht(MeetingDagType.LaatseDag).Should().Be(
            "1. 11:30 (11:30 - 13:30) · 2. 13:30: Eindscène (13:30 - 15:30)");
    }

    [Test]
    public void Overzicht_lists_a_slot_without_a_window_unnumbered()
    {
        _sut.Starttijden =
        [
            Slot(StarttijdIds.Tijd1930Introquest, "19:30: Introquest", 5, 1, eersteDag: true),
            Slot(StarttijdIds.Tijd2000, "20:00", 6, 2, eersteDag: true, vensterEersteDag: "18:00 - 21:00"),
        ];

        _sut.BuildStarttijdOverzicht(MeetingDagType.EersteDag).Should().Be(
            "19:30: Introquest (geen blok) · 1. 20:00 (18:00 - 21:00)",
            "a slot without a window does not take a number, which is exactly why 20:00 is block 1");
    }

    [Test]
    public void Overzicht_says_so_when_a_day_type_has_nothing_left()
    {
        _sut.Starttijden = [Slot(StarttijdIds.Tijd1030, "10:30", 1, 1, middelDagen: true)];

        _sut.BuildStarttijdOverzicht(MeetingDagType.LaatseDag).Should().Be("geen slots beschikbaar");
    }

    private static AdminModel CreateModel(TestDocumentStore documentStore)
    {
        var model = new AdminModel(documentStore, null!, null!, null!, null!, TestReferenceData.Seed, new CapturingLogger<AdminModel>());

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin@example.test")], "TestAuth")),
        };

        model.PageContext = new PageContext(new ActionContext(httpContext, new RouteData(), new PageActionDescriptor()))
        {
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
        };

        return model;
    }

    private static StarttijdConfig Slot(
        string id,
        string displayName,
        int enumValue,
        int order,
        bool eersteDag = false,
        bool middelDagen = false,
        bool laatsteDag = false,
        string vensterEersteDag = "",
        string vensterMiddelDagen = "",
        string vensterLaatsteDag = "",
        bool isNone = false)
        => new ()
        {
            Id = id,
            EnumValue = enumValue,
            DisplayName = displayName,
            IsAvailableOnEersteDag = eersteDag,
            IsAvailableOnMiddelDagen = middelDagen,
            IsAvailableOnLaatsteDag = laatsteDag,
            BlokVensterEersteDag = vensterEersteDag,
            BlokVensterMiddelDagen = vensterMiddelDagen,
            BlokVensterLaatsteDag = vensterLaatsteDag,
            Order = order,
            IsNone = isNone,
        };

    private void SeedZaterdagSlots()
    {
        _documentStore.Seed(
            Slot(StarttijdIds.None, "-", 0, 0, isNone: true),
            Slot(StarttijdIds.Tijd1030, "10:30", 1, 1, middelDagen: true, laatsteDag: true, vensterMiddelDagen: "9:30 - 11:30", vensterLaatsteDag: "9:30 - 11:30"),
            Slot(StarttijdIds.Tijd1230, "12:30", 2, 2, middelDagen: true, laatsteDag: true, vensterMiddelDagen: "11:30 - 13:30", vensterLaatsteDag: "11:30 - 13:30"),
            Slot(StarttijdIds.Tijd1430, "14:30", 3, 3, middelDagen: true, laatsteDag: true, vensterMiddelDagen: "13:30 - 15:30", vensterLaatsteDag: "13:30 - 15:30"));
    }

    private void FillForm(
        string naam,
        bool eersteDag = false,
        bool middelDagen = false,
        bool laatsteDag = false,
        string vensterEersteDag = "",
        string vensterMiddelDagen = "",
        string vensterLaatsteDag = "")
    {
        _sut.EditStarttijdId = null;
        _sut.EditStarttijdNaam = naam;
        _sut.EditStarttijdEersteDag = eersteDag;
        _sut.EditStarttijdMiddelDagen = middelDagen;
        _sut.EditStarttijdLaatsteDag = laatsteDag;
        _sut.EditStarttijdBlokVensterEersteDag = vensterEersteDag;
        _sut.EditStarttijdBlokVensterMiddelDagen = vensterMiddelDagen;
        _sut.EditStarttijdBlokVensterLaatsteDag = vensterLaatsteDag;
    }
}
