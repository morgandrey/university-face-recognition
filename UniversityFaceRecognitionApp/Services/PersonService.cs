using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UniversityFaceRecognitionApp.DataAccess;
using UniversityFaceRecognitionApp.Models;

namespace UniversityFaceRecognitionApp.Services;

public class PersonService
{
    private readonly UniversityDbContext _universityDbContext = new();

    public async Task<List<Person>> FindAllPersonsAsync()
    {
        return await _universityDbContext.Person.ToListAsync();
    }
}