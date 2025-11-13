using LemmikkiAPI;

// Luo WebApplication builderin ja sovelluksen
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5000");
var app = builder.Build();

// Luodaan Asiakaskanta-olio, joka hallinnoi tietokantaa
var Asiakaskanta = new Asiakaskanta();

/// *** GET-pyyntö: hakee lemmikin omistajan puhelinnumeron lemmikin nimen perusteella ***
/// URL-esimerkki: /lemmikki/omistaja?nimi=Rex
app.MapGet("/lemmikki/omistaja", (string nimi) =>
{
    // Kysytään tietokannalta omistajan puhelinnumero
    var phone = Asiakaskanta.SearchOwner(nimi);

    if (phone != null)
    {
        // Jos löytyi, palautetaan OK-status ja tieto JSON-muodossa
        return Results.Ok(new { Lemmikki = nimi, Puhelin = phone });
    }
    else
    {
        // Jos ei löytynyt, palautetaan NotFound-status viestin kanssa
        return Results.NotFound($"Lemmikkiä {nimi} ei löytynyt");
    }
});

/// *** POST-pyyntö: lisää uuden omistajan tietokantaan ***
/// Lähetettävä JSON-esimerkki:
/// {
///    "Nimi": "Matti",
///    "Puhelin": "0401234567",
///    "Osoite": "Esimerkkikatu 1"
/// }
app.MapPost("/omistajat", (Omistaja omistaja) =>
{
    // Lisätään omistaja tietokantaan
    Asiakaskanta.AddOwner(omistaja.Nimi, omistaja.Puhelin, omistaja.Osoite);

    // Palautetaan lista kaikista omistajista
    return Asiakaskanta.GetOwners();
});

/// *** POST-pyyntö: lisää uuden lemmikin tietokantaan ***
/// Lähetettävä JSON-esimerkki:
/// {
///    "Omistajan_id": 1,
///    "Nimi": "Rex",
///    "Laji": "Koira"
/// }
app.MapPost("/lemmikki", (Lemmikki lemmikki) =>
{
    // Lisätään lemmikki tietokantaan
    Asiakaskanta.AddPet(lemmikki.Nimi, lemmikki.Omistajan_id, lemmikki.Laji);

    // Palautetaan lista kaikista lemmikeistä
    return Asiakaskanta.GetPets();
});

app.MapGet("/omistajat", () =>
{
    return Results.Ok(Asiakaskanta.GetOwners());
});

// Käynnistetään sovellus ja odotetaan HTTP-pyyntöjä
app.Run();
