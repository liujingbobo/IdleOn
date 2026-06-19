namespace IdleOn.Dialogue
{
    public interface IDialogueConditionResolver
    {
        bool CanUseChoice(DialogueChoice choice);
    }
}
