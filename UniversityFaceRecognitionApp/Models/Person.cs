namespace UniversityFaceRecognitionApp.Models;

public partial class Person
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string Surname { get; set; } = null!;

    public string? Patronymic { get; set; }

    public byte[] Photo { get; set; } = null!;
}