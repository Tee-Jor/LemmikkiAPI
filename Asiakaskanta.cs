namespace LemmikkiAPI;

using Microsoft.Data.Sqlite;

public record Omistaja (string Nimi, string Puhelin, string Osoite);
public record KaikkiOmistajat(int ID, string Nimi, string Puhelin, string Osoite);
public record Lemmikki(int Omistajan_id, string Nimi, string Laji);
public record KaikkiLemmikit(int Lemmikin_id, int Omistajan_id, string Nimi, string Laji);

/// <summary>
/// DataBase-luokka hallinnoi SQLite-tietokantaa ja tarjoaa metodeja
/// omistajien ja lemmikkien lisäämiseen, päivittämiseen ja hakemiseen.
/// </summary>
public class Asiakaskanta
{
    // Yhteysmerkkijono SQLite-tietokantaan. Tämä kertoo ohjelmalle, missä
    // tietokantatiedosto sijaitsee ja mikä sen nimi on.
    private static string _connectionString = "Data Source=Data.db";

    /// <summary>
    /// Konstruktori, joka alustaa tietokannan ja luo tarvittavat taulut, jos niitä ei ole.
    /// Tämä mahdollistaa ohjelman ajamisen ensimmäistä kertaa ilman valmiita tietokantoja.
    /// </summary>
    public Asiakaskanta()
    {
        // "using" varmistaa, että yhteys suljetaan automaattisesti lohkon lopussa,
        // vaikka poikkeus tapahtuisi.
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            // Luodaan Omistaja-taulu, jos sitä ei ole olemassa.
            // PRIMARY KEY määrittää Omistajan_id:n yksilölliseksi tunnisteeksi.
            var createOmistajaTableCmd = connection.CreateCommand();
            createOmistajaTableCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Omistaja (
                    Omistajan_id INTEGER PRIMARY KEY,
                    Puhelin TEXT,
                    Osoite TEXT,
                    Nimi TEXT
                )";
            createOmistajaTableCmd.ExecuteNonQuery();

            // Luodaan Lemmikki-taulu, jos sitä ei ole olemassa.
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
    /// Tarkistaa ensin, ettei samalla puhelinnumerolla ole jo olemassa olevaa omistajaa.
    /// </summary>
    public void AddOwner(string nimi, string puhelin, string osoite)
    {
        // Tarkistetaan, että kaikki kentät on täytetty.
        if (string.IsNullOrEmpty(nimi) || string.IsNullOrEmpty(puhelin) || string.IsNullOrEmpty(osoite))
        {
            throw new ArgumentException("Kaikki kentät täytettävä");
        }

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            // Parametrisoitu SELECT tarkistaa, onko omistaja jo olemassa puhelinnumeron perusteella.
            // @Puhelin toimii paikkamerkkinä SQL-lauseessa.
            // Tämä estää SQL-injektion ja varmistaa, että syöte käsitellään oikein.
            var commandForOmistaja = connection.CreateCommand();
            commandForOmistaja.CommandText = "SELECT Omistajan_id FROM Omistaja WHERE Puhelin = @Puhelin";
            commandForOmistaja.Parameters.AddWithValue("@Puhelin", puhelin);

            // ExecuteScalar palauttaa yhden arvon, tässä tapauksessa Omistajan_id:n tai null.
            object? omistajanId = commandForOmistaja.ExecuteScalar();
            if (omistajanId != null)
            {
                throw new InvalidOperationException("Tällä puhelinnumerolla löytyy jo henkilö.");
            }
            else
            {
                // Lisätään uusi omistaja, koska samaa puhelinnumeroa ei löydy.
                var insertOmistajaCommand = connection.CreateCommand();
                insertOmistajaCommand.CommandText = @"
                    INSERT INTO Omistaja (Nimi, Puhelin, Osoite) VALUES (@Nimi, @Puhelin, @Osoite)";

                // Parametrit liitetään paikkamerkkeihin SQL-lauseessa.
                // Tämä tekee koodista turvallisemman ja käsittelee eri tietotyypit oikein.
                insertOmistajaCommand.Parameters.AddWithValue("@Nimi", nimi);
                insertOmistajaCommand.Parameters.AddWithValue("@Puhelin", puhelin);
                insertOmistajaCommand.Parameters.AddWithValue("@Osoite", osoite);

                insertOmistajaCommand.ExecuteNonQuery();


            }
        }
    }

    /// <summary>
    /// Lisää lemmikin tietokantaan ja liittää sen olemassa olevaan omistajaan.
    /// Käyttäjä valitsee omistajan ID:n listasta.
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

            var checkOwnerCmd = connection.CreateCommand();
            checkOwnerCmd.CommandText = "SELECT COUNT(*) FROM Omistaja WHERE Omistajan_id = @Omistajan_id";
            checkOwnerCmd.Parameters.AddWithValue("@Omistajan_id", omistajanId);
            int ownerExists = Convert.ToInt32(checkOwnerCmd.ExecuteScalar());
            if (ownerExists == 0)
            {
                throw new ArgumentException("Annetulla ID:llä ei löydy omistajaa");
            }

            // Lisätään lemmikki tietokantaan parametrisoidulla INSERT-lauseella.
            var insertLemmikki = connection.CreateCommand();
            insertLemmikki.CommandText = @"
                INSERT INTO Lemmikki (Omistajan_id, Nimi, Laji)
                VALUES (@Omistajan_id, @Nimi, @Laji)";

            // Parametrien käyttö tekee koodista turvallista ja oikeaoppista.
            insertLemmikki.Parameters.AddWithValue("@Omistajan_id", omistajanId);
            insertLemmikki.Parameters.AddWithValue("@Nimi", nimi);
            insertLemmikki.Parameters.AddWithValue("@Laji", laji);

            insertLemmikki.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Päivittää olemassa olevan omistajan puhelinnumeron.
    /// Parametrit estävät SQL-injektion ja käsittelevät käyttäjän syötteen oikein.
    /// </summary>
    public void NewNumber()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            // Listataan kaikki omistajat käyttäjälle, jotta hän näkee ID:t.
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

            Console.WriteLine("Anna vaihdettava puhelinnumero:");
            string? VanhaNumero = Console.ReadLine();

            Console.WriteLine("Anna uusi puhelinnumero:");
            string? UusiNumero = Console.ReadLine();

            if (string.IsNullOrEmpty(VanhaNumero) || string.IsNullOrEmpty(UusiNumero))
            {
                Console.WriteLine("Molempien puhelinnumerokenttien tulee olla täytetty.");
                return;
            }

            // Tarkistetaan, löytyykö vanha numero tietokannasta.
            var checkNumberCmd = connection.CreateCommand();
            checkNumberCmd.CommandText = "SELECT COUNT(*) FROM Omistaja WHERE Puhelin = @Puhelin";
            checkNumberCmd.Parameters.AddWithValue("@Puhelin", VanhaNumero);
            int count = Convert.ToInt32(checkNumberCmd.ExecuteScalar());

            if (count == 0)
            {
                Console.WriteLine($"Numeroa {VanhaNumero} ei löytynyt tietokannasta.");
                return;
            }

            // Päivitetään puhelinnumero turvallisesti parametrien avulla.
            var NewNumberCmd = connection.CreateCommand();
            NewNumberCmd.CommandText = "UPDATE Omistaja SET Puhelin = @UusiNumero WHERE Puhelin = @VanhaNumero";
            // @UusiNumero toimii paikkamerkkinä, joka korvataan UusiNumero-arvolla.
            NewNumberCmd.Parameters.AddWithValue("@UusiNumero", UusiNumero);
            // @VanhaNumero toimii paikkamerkkinä, joka korvataan VanhaNumero-arvolla.
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
    /// Parametrit estävät SQL-injektion ja käsittelevät käyttäjän syötteen oikein.
    /// </summary>
    public void SearchPet()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            // Listataan kaikki omistajat käyttäjälle, jotta ID:n valinta on helpompaa.
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
    /// Käyttää JOIN-lausetta yhdistämään Lemmikki- ja Omistaja-taulut.
    /// Parametrien käyttö tekee hausta turvallisen.
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
                            reader.GetString(1), // Nimi
                            reader.GetString(2), // Puhelin
                            reader.GetString(3)  // Osoite
                        );
                        omistajat.Add(omistaja);
                    }
                    return omistajat;
                }
            }
        }


    public List<KaikkiLemmikit> GetPets()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM Lemmikki";
            using (var reader = selectCmd.ExecuteReader())
            {
                var omistajat = new List<KaikkiLemmikit>();
                while (reader.Read())
                {
                    var omistaja = new KaikkiLemmikit(
                        reader.GetInt32(0),  // Lemmikin ID
                        reader.GetInt32(1), // Omistajan ID
                        reader.GetString(2), // Nimi
                        reader.GetString(3)  // Laji
                       
                    );
                    omistajat.Add(omistaja);
                }
                return omistajat;
            }
        }
      
    }
}