using System;
using System.Collections.Generic;

public class UserProfile
{
    public string Name { get; set; }
    public int Age { get; set; }
    public DateTime BirthDay { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string CatNum { get; set; }
    public List<string> CatTypes { get; set; }
    public bool HasCat { get; set; }
    public bool PlayWithCat { get; set; }
    public string Language { get; set; }
}