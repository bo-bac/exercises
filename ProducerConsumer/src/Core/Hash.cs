namespace Core;

public class Hash
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Sha1 { get; set; } = default!;
}
