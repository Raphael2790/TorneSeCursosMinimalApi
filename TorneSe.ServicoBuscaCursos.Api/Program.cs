using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var securityScheme = new OpenApiSecurityScheme()
{
    Name = "Authorization",
    Type = SecuritySchemeType.ApiKey,
    Scheme = "Bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "JSON Web Token based security",
};

var securityReq = new OpenApiSecurityRequirement()
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        new string[] {}
    }
};

var contact = new OpenApiContact()
{
    Name = "Raphael Silvestre",
    Email = "tornese@email.com",
    Url = new Uri("http://localhost:7295")
};

var license = new OpenApiLicense()
{
    Name = "Free License",
    Url = new Uri("https://torneseumprogramador.com")
};

var info = new OpenApiInfo()
{
    Version = "v1",
    Title = "Minimal API - JWT Authentication with Swagger demo",
    Description = "Implementing JWT Authentication in Minimal API",
    TermsOfService = new Uri("http://www.example.com"),
    Contact = contact,
    License = license
};

builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey
            (Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true
    };
});

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", info);
    o.AddSecurityDefinition("Bearer", securityScheme);
    o.AddSecurityRequirement(securityReq);
});

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.MapGet("/obterCurso", [AllowAnonymous] (int alunoId, int professorId, int atividadeId) =>
{
    string[] nomesCursos = { "Java", "CSharp", "JavaScript", "Rust", "Go" };
    string[] alunos = { "Pedro", "Jo�o", "Ana", "Maria", "Gabriel", "Antonio", "Marcela", "Lucas", "Lais" };
    string[] professores = { "Danilo", "Raphael", "Douglas", "Vinicius", "Bruno" };
    string[] atividades = { "POO", "L�gica de Programa��o", "Estrutura de dados", "Algoritmos", "Trabalhando com cole��es" };
    
    int contador = 0;

    return Results.Ok(nomesCursos.Select((nomeCurso, index) =>
        new Curso
        (
            ++contador,
            nomeCurso,
            true,
            DateTime.Now.AddMonths(-contador),
            DateTime.Now.AddMonths(contador),
            atividades.Select((nomeAtividade, indexAtividade) => 
            new Atividade(atividadeId + indexAtividade, nomeAtividade, true, DateTime.Now.AddMonths(-contador), DateTime.Now.AddMonths(contador),new Professor(professorId+indexAtividade, professores[indexAtividade], true))).ToList(),
            alunos.Select((nome, indexAluno) => new Aluno(alunoId+indexAluno, nome, true)).ToList()
        ))
        .ToArray());
})
.WithName("ObterCurso");

app.MapPost("/security/getToken", [AllowAnonymous] (UserDto user) =>
{

    if (user.UserName == "torneseumprogramador" && user.Password == "sdfeergbjhjjmnnm")
    {
        var issuer = builder.Configuration["Jwt:Issuer"];
        var audience = builder.Configuration["Jwt:Audience"];
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var jwtTokenHandler = new JwtSecurityTokenHandler();

        var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);

        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("Id", "1"),
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Email, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
           
            Expires = DateTime.UtcNow.AddHours(6),
            Audience = audience,
            Issuer = issuer,
           
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature)
        };

        var token = jwtTokenHandler.CreateToken(tokenDescriptor);

        var jwtToken = jwtTokenHandler.WriteToken(token);

        return Results.Ok(jwtToken);
    }
    else
    {
        return Results.Unauthorized();
    }
}).WithName("ObterToken");

app.Run();

public class Curso
{
    public Curso
        (int id, 
        string nome, 
        bool ativo, 
        DateTime dataInicio, 
        DateTime dataTermino, 
        ICollection<Atividade> atividades, 
        ICollection<Aluno> alunos)
    {
        Id = id;
        Nome = nome;
        Ativo = ativo;
        DataInicio = dataInicio;
        DataTermino = dataTermino;
        Atividades = atividades;
        Alunos = alunos;
    }

    public int Id { get; set; }
    public string Nome { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataTermino { get; set; }
    public ICollection<Atividade> Atividades { get; set; }
    public ICollection<Aluno> Alunos { get; set; }
}

public class Atividade
{
    public Atividade(int id, string nome, bool ativo, DateTime dataInicio, DateTime dataTermino, Professor professor)
    {
        Id = id;
        Nome = nome;
        Ativo = ativo;
        DataInicio = dataInicio;
        DataTermino = dataTermino;
        Professor = professor;
    }

    public int Id { get; set; }
    public string Nome { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataTermino { get; set; }
    public Professor Professor { get; set; }
}

public class Professor
{
    public Professor(int id, string nome, bool ativo)
    {
        Id = id;
        Nome = nome;
        Ativo = ativo;
    }

    public int Id { get; set; }
    public string Nome { get; set; }
    public bool Ativo { get; set; }
}

public class Aluno
{
    public Aluno(int id, string nome, bool ativo)
    {
        Id = id;
        Nome = nome;
        Ativo = ativo;
    }

    public int Id { get; set; }
    public string Nome { get; set; }
    public bool Ativo { get; set; }
}

record UserDto(string UserName, string Password);