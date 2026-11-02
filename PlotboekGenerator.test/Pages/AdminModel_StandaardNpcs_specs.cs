using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace PlotboekGenerator.test.Pages;

/// <summary>
/// Admin → Standaard-NPC's: create, edit, rename and delete the role templates. Plots keep their own
/// copy, so the only thing to protect here is the list itself: one record per name.
/// </summary>
public sealed class AdminModel_StandaardNpcs_specs
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
    public async Task Creates_a_standaard_npc_keyed_on_its_name()
    {
        FillForm(rol: "Stadswacht", kleding: "tabbaard, helm", casting: "stevig postuur");

        await _sut.HandleSaveStandaardNpc();

        _documentStore.Load<StandaardNpc>("Stadswacht").Should().BeEquivalentTo(new
        {
            Rol = "Stadswacht",
            KledingEnSchmink = "tabbaard, helm",
            CastingInstructie = "stevig postuur",
            GewijzigdDoor = "admin@example.test",
        });
        _sut.ErrorMessage.Should().BeNull();
        _sut.SuccessMessage.Should().Contain("aangemaakt");
    }

    [Test]
    public async Task Refuses_to_create_a_name_that_already_exists_in_any_case()
    {
        _documentStore.Seed(Stadswacht());
        FillForm(rol: "STADSWACHT", kleding: "iets anders");

        await _sut.HandleSaveStandaardNpc();

        _sut.ErrorMessage.Should().Contain("bestaat al");
        _documentStore.Load<StandaardNpc>("Stadswacht").KledingEnSchmink.Should().Be(
            "tabbaard, helm",
            "creating must never overwrite; editing is a separate, explicit action");
    }

    [TestCase("")]
    [TestCase("!!!")]
    public async Task Refuses_a_name_without_letters_or_digits(string rol)
    {
        FillForm(rol: rol);

        await _sut.HandleSaveStandaardNpc();

        _sut.ErrorMessage.Should().NotBeNullOrEmpty();
        _documentStore.GetAll<StandaardNpc>().Should().BeEmpty();
    }

    [Test]
    public async Task Edits_in_place_when_the_name_keeps_its_key()
    {
        _documentStore.Seed(Stadswacht());
        FillForm(id: "Stadswacht", rol: "stadswacht", kleding: "maliën, helm");

        await _sut.HandleSaveStandaardNpc();

        _documentStore.GetAll<StandaardNpc>().Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Id = "Stadswacht", Rol = "stadswacht", KledingEnSchmink = "maliën, helm" });
        _sut.SuccessMessage.Should().Contain("bijgewerkt");
    }

    [Test]
    public async Task Renaming_moves_the_record_to_the_new_key()
    {
        _documentStore.Seed(Stadswacht());
        FillForm(id: "Stadswacht", rol: "Wachter", kleding: "tabbaard, helm");

        await _sut.HandleSaveStandaardNpc();

        _documentStore.GetAll<StandaardNpc>().Should().ContainSingle()
            .Which.Id.Should().Be(
                "Wachter",
                "the key must keep matching the name, or saving 'Stadswacht' later would overwrite 'Wachter'");
    }

    [Test]
    public async Task Renaming_onto_an_existing_name_is_refused()
    {
        _documentStore.Seed(Stadswacht(), StandaardNpc.Create("Bandiet", "lompen", string.Empty, "anna", DateTime.UtcNow));
        FillForm(id: "Bandiet", rol: "Stadswacht");

        await _sut.HandleSaveStandaardNpc();

        _sut.ErrorMessage.Should().Contain("bestaat al");
        _documentStore.GetAll<StandaardNpc>().Select(s => s.Id).Should().BeEquivalentTo(["Stadswacht", "Bandiet"]);
    }

    [Test]
    public async Task Editing_a_standaard_npc_that_is_gone_is_refused()
    {
        FillForm(id: "Stadswacht", rol: "Stadswacht");

        await _sut.HandleSaveStandaardNpc();

        _sut.ErrorMessage.Should().Be("Standaard-NPC niet gevonden.");
        _documentStore.GetAll<StandaardNpc>().Should().BeEmpty(because: "an edit must not recreate a deleted record");
    }

    [Test]
    public async Task Deletes_a_standaard_npc()
    {
        _documentStore.Seed(Stadswacht());
        _sut.DeleteStandaardNpcId = "Stadswacht";

        await _sut.HandleDeleteStandaardNpc();

        _documentStore.GetAll<StandaardNpc>().Should().BeEmpty();
        _sut.SuccessMessage.Should().Contain("verwijderd");
    }

    private static StandaardNpc Stadswacht()
        => StandaardNpc.Create("Stadswacht", "tabbaard, helm", "stevig postuur", "anna", DateTime.UtcNow);

    private void FillForm(string rol, string id = "", string kleding = "", string casting = "")
    {
        _sut.EditStandaardNpcId = id;
        _sut.EditStandaardNpcRol = rol;
        _sut.EditStandaardNpcKledingEnSchmink = kleding;
        _sut.EditStandaardNpcCastingInstructie = casting;
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
}
