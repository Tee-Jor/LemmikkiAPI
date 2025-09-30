using LemmikkiAPI;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var Asiakaskanta = new Asiakaskanta();

app.MapGet("/lemmikki/omistaja", (string nimi) =>
{
    var phone = Asiakaskanta.SearchOwner(nimi);
    if (phone != null)
    {
        return Results.Ok(new { Lemmikki = nimi, Puhelin = phone });
    }
    else
    {
        return Results.NotFound($"Lemmikkiä {nimi} ei löytynyt");
    }
});


app.MapPost("/omistajat", (Omistaja omistaja) =>
{
    Asiakaskanta.AddOwner(omistaja.Nimi, omistaja.Puhelin, omistaja.Osoite);
    return Asiakaskanta.GetOwners();
});

app.MapPost("/lemmikki", (Lemmikki lemmikki) =>
{
    Asiakaskanta.AddPet(lemmikki.Nimi, lemmikki.Omistajan_id, lemmikki.Laji);
    return Asiakaskanta.GetPets();
});


app.Run();
