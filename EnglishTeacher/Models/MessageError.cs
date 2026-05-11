namespace EnglishTeacher.Models
{
    public class MessageError
    {
        public string Codigo { get; set; }
        public string Mensagem { get; set; }

        public List<string> ToReceipt()
        {
            var result = new List<string>
        {
            $"Código: {Codigo}",
            $"Mensagem: {Mensagem}",
        };
            return result;
        }
    }
}
