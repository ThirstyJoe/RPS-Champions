namespace ThirstyJoe.RPSChampions
{
    public class TitleDescriptionPair
    {
        public string Title;
        public string Description;

        public TitleDescriptionPair(string title, string description = "")
        {
            Title = title;
            Description = description;
        }
    }
}