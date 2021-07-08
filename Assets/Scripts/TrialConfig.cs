/* Represents the configuration of the simulation */
public class TrialConfig
{
    public string ParticipantName { get; }
    public Path Path { get; }
    public Advice Advice { get; }

    public TrialConfig(string participantName, Path path, Advice advice)
    {
        ParticipantName = participantName;
        Path = path;
        Advice = advice;
    }
}