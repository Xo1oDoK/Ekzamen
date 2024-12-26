using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace CardEncryption
{
    // Интерфейс для шифрования
    public interface IEncryptionService
    {
        string Encrypt(string plainText, string key);
        
    }

    // Интерфейс для хэширования
    public interface IHashingService
    {
        string Hash(string input, string salt);
    }

    // AES-шифрования
    public class AesEncryptionService : IEncryptionService
    {
        public string Encrypt(string plainText, string key)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32));
            aes.GenerateIV();

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            var iv = aes.IV;
            var encryptedContent = ms.ToArray();
            var result = Convert.ToBase64String(iv) + ":" + Convert.ToBase64String(encryptedContent);
            return result;
        }

    }

    // Реализация SHA256-хэширования
    public class SHA256HashingService : IHashingService
    {
        public string Hash(string input, string salt)
        {
            using var sha256 = SHA256.Create();
            var inputWithSalt = input + salt;
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(inputWithSalt));
            return Convert.ToBase64String(hashBytes);
        }
    }

    public class CardProcessor
    {
        private readonly IEncryptionService _encryptionService;
        private readonly IHashingService _hashingService;
        private readonly string _encryptionKey = "G7f9aK4pXq8Z2NvLmWjYrT1BdVQoCh5E"; 
        private readonly string _salt = "A1fG7kZpXq9W2LvM"; 

        public CardProcessor(IEncryptionService encryptionService, IHashingService hashingService)
        {
            _encryptionService = encryptionService;
            _hashingService = hashingService;
        }

        public void ProcessCards(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var cards = JsonConvert.DeserializeObject<CardsContainer>(json)?.Cards;

            if (cards == null)
            {
                Console.WriteLine("Файл JSON пуст или имеет неверный формат.");
                return;
            }

            foreach (var card in cards)
            {
                card.Cvc = _hashingService.Hash(card.Cvc, _salt);
                card.Name = _encryptionService.Encrypt(card.Name, _encryptionKey);
                card.Family = _encryptionService.Encrypt(card.Family, _encryptionKey);
                card.Number = _encryptionService.Encrypt(card.Number, _encryptionKey);
                card.Month = _encryptionService.Encrypt(card.Month, _encryptionKey);
                card.Year = _encryptionService.Encrypt(card.Year, _encryptionKey);
            }

            var encryptedJson = JsonConvert.SerializeObject(new CardsContainer { Cards = cards }, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath.Replace(".json", "EncryptedCards.json"), encryptedJson);
        }
    }
    // Модель для контейнера карточек
    public class CardsContainer
    {
        [JsonProperty("cards")]
        public List<Card> Cards { get; set; }
    }

    // Модель данных карточки
    public class Card
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("family")]
        public string Family { get; set; }

        [JsonProperty("cvc")]
        public string Cvc { get; set; }

        [JsonProperty("month")]
        public string Month { get; set; }

        [JsonProperty("year")]
        public string Year { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var filePath = @"C:\my\загрузки яндекса\экз\Card.json";

            var encryptionService = new AesEncryptionService();
            var hashingService = new SHA256HashingService();
            var cardProcessor = new CardProcessor(encryptionService, hashingService);

            cardProcessor.ProcessCards(filePath);

            Console.WriteLine("Карты успешно обработаны и сохранены в EncryptedCards.json");
        }
    }
}
