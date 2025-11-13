namespace LemmikkiAPI;

using Microsoft.Data.Sqlite;

// *** Recordit tietorakenteina ***
// Record on kevyempi tapa määritellä tietorakenne, jolla on kentät, getterit ja setterit valmiina.

public record Omistaja(string Nimi, string Puhelin, string Osoite);
public record KaikkiOmistajat(int ID, string Nimi, string Puhelin, string Osoite);
public record Lemmikki(int Omistajan_id, string Nimi, string Laji);
public record KaikkiLemmikit(int Lemmikin_id, int Omistajan_id, string Nimi, string Laji);

/// <summary>
/// Asiakaskanta-luokka hallinnoi SQLite-tietokantaa ja tarjoaa
/// metodeja omistajien ja lemmikkien lisäämiseen, päivittämiseen ja hakemiseen.
/// </summary>
public class Asiakaskanta
{
    // Yhteysmerkkijono kertoo, mistä SQLite-tietokanta löytyy.
    private static string _connectionString = "Data Source=Data.db";

    /// <summary>
    /// Konstruktori: Luo tietokannan taulut, jos niitä ei vielä ole.
    /// Tämä mahdollistaa ohjelman ajamisen ensimmäistä kertaa ilman olemassa olevaa tietokantaa.
    /// </summary>
    public Asiakaskanta()
    {
        // "using" varmistaa, että tietokantayhteys suljetaan automaattisesti
        // lohkon lopussa, vaikka ohjelma kohtaisi virheen.
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            // Luodaan Omistaja-taulu, jos sitä ei ole.
            var createOmistajaTableCmd = connection.CreateCommand();
            createOmistajaTableCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Omistaja (
                    Omistajan_id INTEGER PRIMARY KEY,
                    Puhelin TEXT,
                    Osoite TEXT,
                    Nimi TEXT
                )";
            createOmistajaTableCmd.ExecuteNonQuery();

            // Luodaan Lemmikki-taulu, jos sitä ei ole.
            // FOREIGN KEY varmistaa, että jokaisella lemmikillä on olemassa oleva omistaja.
            var createLemmikkiTableCmd = connection.CreateCommand();
            createLemmikkiTableCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Lemmikki(
                    Lemmikin_id INTEGER PRIMARY KEY,
                    Omistajan_id INTEGER,
                    Nimi TEXT,
                    Laji TEXT,
                    FOREIGN KEY (Omistajan_id) REFERENCES Omistaja(Omistajan_id)
                )";
            createLemmikkiTableCmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Lisää uuden omistajan tietokantaan.
    /// Ensin tarkistetaan, ettei samalla puhelinnumerolla ole jo olemassa olevaa omistajaa.
    /// </summary>
    public void AddOwner(string nimi, string puhelin, string osoite)
    {
        // Varmistetaan, että kaikki kentät on täytetty.
        if (string.IsNullOrEmpty(nimi) || string.IsNullOrEmpty(puhelin) || string.IsNullOrEmpty(osoite))
        {
            throw new ArgumentException("Kaikki kentät täytettävä");
        }

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            // Tarkistetaan, onko sama puhelinnumero jo olemassa.
            var commandForOmistaja = connection.CreateCommand();
            commandForOmistaja.CommandText = "SELECT Omistajan_id FROM Omistaja WHERE Puhelin = @Puhelin";
            commandForOmistaja.Parameters.AddWithValue("@Puhelin", puhelin); // Parametri estää SQL-injektion

            object? omistajanId = commandForOmistaja.ExecuteScalar(); // Palauttaa yhden arvon tai null
            if (omistajanId != null)
            {
                throw new InvalidOperationException("Tällä puhelinnumerolla löytyy jo henkilö.");
            }
            else
            {
                // Lisätään uusi omistaja, koska samaa puhelinnumeroa ei löytynyt
                var insertOmistajaCommand = connection.CreateCommand();
                insertOmistajaCommand.CommandText = @"
                    INSERT INTO Omistaja (Nimi, Puhelin, Osoite) VALUES (@Nimi, @Puhelin, @Osoite)";

                insertOmistajaCommand.Parameters.AddWithValue("@Nimi", nimi);
                insertOmistajaCommand.Parameters.AddWithValue("@Puhelin", puhelin);
                insertOmistajaCommand.Parameters.AddWithValue("@Osoite", osoite);

                insertOmistajaCommand.ExecuteNonQuery();
            }
        }
    }

    /// <summary>
    /// Lisää lemmikin tietokantaan ja liittää sen olemassa olevaan omistajaan.
    /// </summary>
    public void AddPet(string nimi, int omistajanId, string laji)
    {
        if (string.IsNullOrEmpty(nimi) || string.IsNullOrEmpty(laji))
        {
            throw new ArgumentException("Lemmikin nimi ja laji ovat pakolliset");
        }

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            // Tarkistetaan, että annettu omistajan ID löytyy tietokannasta
            var checkOwnerCmd = connection.CreateCommand();
            checkOwnerCmd.CommandText = "SELECT COUNT(*) FROM Omistaja WHERE Omistajan_id = @Omistajan_id";
            checkOwnerCmd.Parameters.AddWithValue("@Omistajan_id", omistajanId);
            int ownerExists = Convert.ToInt32(checkOwnerCmd.ExecuteScalar());

            if (ownerExists == 0)
            {
                throw new ArgumentException("Annetulla ID:llä ei löydy omistajaa");
            }

            // Lisätään lemmikki tietokantaan
            var insertLemmikki = connection.CreateCommand();
            insertLemmikki.CommandText = @"
                INSERT INTO Lemmikki (Omistajan_id, Nimi, Laji)
                VALUES (@Omistajan_id, @Nimi, @Laji)";
            insertLemmikki.Parameters.AddWithValue("@Omistajan_id", omistajanId);
            insertLemmikki.Parameters.AddWithValue("@Nimi", nimi);
            insertLemmikki.Parameters.AddWithValue("@Laji", laji);

            insertLemmikki.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Päivittää olemassa olevan omistajan puhelinnumeron.
    /// </summary>
    public void NewNumber()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            // Listataan kaikki omistajat, jotta käyttäjä näkee heidän ID:t
            var selectOwners = connection.CreateCommand();
            selectOwners.CommandText = "SELECT Omistajan_id, Nimi, Puhelin FROM Omistaja";

            using (var reader = selectOwners.ExecuteReader())
            {
                Console.WriteLine("******* Omistajat ********");
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string nimi = reader.GetString(1);
                    string puhelin = reader.GetString(2);
                    Console.WriteLine($"Nimi: {nimi} - Puhelin: {puhelin} - ID: {id}");
                }
                Console.WriteLine("****************************\n");
            }

            // Käyttäjältä kysytään vanha ja uusi puhelinnumero
            Console.WriteLine("Anna vaihdettava puhelinnumero:");
            string? VanhaNumero = Console.ReadLine();

            Console.WriteLine("Anna uusi puhelinnumero:");
            string? UusiNumero = Console.ReadLine();

            if (string.IsNullOrEmpty(VanhaNumero) || string.IsNullOrEmpty(UusiNumero))
            {
                Console.WriteLine("Molempien puhelinnumerokenttien tulee olla täytetty.");
                return;
            }

            // Tarkistetaan, löytyykö vanha numero tietokannasta
            var checkNumberCmd = connection.CreateCommand();
            checkNumberCmd.CommandText = "SELECT COUNT(*) FROM Omistaja WHERE Puhelin = @Puhelin";
            checkNumberCmd.Parameters.AddWithValue("@Puhelin", VanhaNumero);
            int count = Convert.ToInt32(checkNumberCmd.ExecuteScalar());

            if (count == 0)
            {
                Console.WriteLine($"Numeroa {VanhaNumero} ei löytynyt tietokannasta.");
                return;
            }

            // Päivitetään puhelinnumero
            var NewNumberCmd = connection.CreateCommand();
            NewNumberCmd.CommandText = "UPDATE Omistaja SET Puhelin = @UusiNumero WHERE Puhelin = @VanhaNumero";
            NewNumberCmd.Parameters.AddWithValue("@UusiNumero", UusiNumero);
            NewNumberCmd.Parameters.AddWithValue("@VanhaNumero", VanhaNumero);

            int affectedRows = NewNumberCmd.ExecuteNonQuery();

            if (affectedRows > 0)
            {
                Console.WriteLine($"Numero {VanhaNumero} on vaihdettu onnistuneesti numeroksi {UusiNumero}.");
            }
            else
            {
                Console.WriteLine("Päivitys epäonnistui.");
            }
        }
    }

    /// <summary>
    /// Hakee ja tulostaa lemmikit annetun omistajan ID:n perusteella.
    /// </summary>
    public void SearchPet()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            // Näytetään kaikki omistajat, jotta käyttäjä voi valita oikean ID:n
            var selectOwners = connection.CreateCommand();
            selectOwners.CommandText = "SELECT Omistajan_id, Nimi, Puhelin FROM Omistaja";

            using (var reader = selectOwners.ExecuteReader())
            {
                Console.WriteLine("******* Omistajat ********");
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string nimi = reader.GetString(1);
                    string puhelin = reader.GetString(2);
                    Console.WriteLine($"Nimi: {nimi} - Puhelin: {puhelin} - ID: {id}");
                }
                Console.WriteLine("****************************\n");
            }

            // Käyttäjä antaa ID:n ja ohjelma hakee lemmikit
            Console.WriteLine("Anna omistajan ID, jonka lemmikit haluat hakea:");
            if (int.TryParse(Console.ReadLine(), out int omistajanId))
            {
                var selectPetsCmd = connection.CreateCommand();
                selectPetsCmd.CommandText = "SELECT Nimi, Laji FROM Lemmikki WHERE Omistajan_id = @Omistajan_id";
                selectPetsCmd.Parameters.AddWithValue("@Omistajan_id", omistajanId);

                using (var petReader = selectPetsCmd.ExecuteReader())
                {
                    if (petReader.HasRows)
                    {
                        Console.WriteLine($"\nLemmikit omistajalle ID {omistajanId}:");
                        while (petReader.Read())
                        {
                            string nimi = petReader.GetString(0);
                            string laji = petReader.GetString(1);
                            Console.WriteLine($"- {nimi}, laji: {laji}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Lemmikkejä ei löytynyt tällä omistaja-ID:llä.");
                    }
                }
            }
            else
            {
                Console.WriteLine("Virheellinen ID. Anna kokonaisluku.");
            }
        }
    }

    /// <summary>
    /// Hakee omistajan puhelinnumeron lemmikin nimen perusteella.
    /// </summary>
    public string? SearchOwner(string petName)
    {
        if (string.IsNullOrEmpty(petName))
        {
            return null;
        }

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var SearchOwner = connection.CreateCommand();
            SearchOwner.CommandText = @"
            SELECT Omistaja.Puhelin
            FROM Lemmikki
            JOIN Omistaja ON Lemmikki.Omistajan_id = Omistaja.Omistajan_id
            WHERE Lemmikki.Nimi = @Nimi";

            SearchOwner.Parameters.AddWithValue("@Nimi", petName);

            object? phoneNumber = SearchOwner.ExecuteScalar();

            return phoneNumber?.ToString();
        }
    }

    /// <summary>
    /// Palauttaa listan kaikista omistajista.
    /// </summary>
    public List<KaikkiOmistajat> GetOwners()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM Omistaja";
            using (var reader = selectCmd.ExecuteReader())
            {
                var omistajat = new List<KaikkiOmistajat>();
                while (reader.Read())
                {
                    var omistaja = new KaikkiOmistajat(
                    reader.GetInt32(0),  // ID
                    reader.GetString(3), // Nimi
                    reader.GetString(1), // Puhelin
                    reader.GetString(2)  // Osoite
                    );

                    omistajat.Add(omistaja);
                }
                return omistajat;
            }
        }
    }

    /// <summary>
    /// Palauttaa listan kaikista lemmikeistä.
    /// </summary>
    public List<KaikkiLemmikit> GetPets()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM Lemmikki";
            using (var reader = selectCmd.ExecuteReader())
            {
                var lemmikit = new List<KaikkiLemmikit>();
                while (reader.Read())
                {
                    var lemmikki = new KaikkiLemmikit(
                        reader.GetInt32(0),  // Lemmikin ID
                        reader.GetInt32(1),  // Omistajan ID
                        reader.GetString(2), // Nimi
                        reader.GetString(3)  // Laji
                    );
                    lemmikit.Add(lemmikki);
                }
                return lemmikit;
            }
        }
    }
}
