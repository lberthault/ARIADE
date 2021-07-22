/* Represents the configuration of the simulation */
public class TrialConfig
{
    public string ParticipantName { get; }
    public Path.PathName PathName { get; }
    public Advice Advice { get; }

    public TrialConfig(string participantName, Path.PathName pathName, Advice advice)
    {
        ParticipantName = participantName;
        PathName = pathName;
        Advice = advice;
    }
}