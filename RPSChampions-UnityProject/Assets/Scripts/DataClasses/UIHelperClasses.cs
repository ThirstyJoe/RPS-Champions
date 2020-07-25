namespace ThirstyJoe.RPSChampions
{
    public class TitleDescriptionButtonData
    {
        public bool ShowRatedIcon;
        public string Title;
        public string Description;
        public string LinkID;

        public TitleDescriptionButtonData(string linkID, string title, string description = "", bool showRatedIcon = false)
        {
            Title = title;
            Description = description;
            LinkID = linkID;
            ShowRatedIcon = showRatedIcon;
        }
    }
}