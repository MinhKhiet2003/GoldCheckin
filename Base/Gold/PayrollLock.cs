namespace Base.Gold;

public class PayrollLock
{
    public int Id { get; set; }
    public DateTime LockedAt { get; set; }
    public Guid LockedBy { get; set; }
}
public class ScheduleLock
{
    public int Id { get; set; }
    public DateTime LockedAt { get; set; }
    public Guid LockedBy { get; set; }
}
