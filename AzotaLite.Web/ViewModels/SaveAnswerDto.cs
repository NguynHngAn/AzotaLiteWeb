namespace AzotaLite.Web.ViewModels
{
    public class SaveAnswerDto
    {
        public int QuestionId { get; set; }
        public int? ChoiceId { get; set; }      // MultipleChoice
        public string? TextAnswer { get; set; } // ShortAnswer
    }
}
