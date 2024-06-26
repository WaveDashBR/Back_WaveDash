﻿using DataAccess;
using Domain;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaBI.API.Services;
using System.Collections.Concurrent;

namespace PlataformaBI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowOrigin")]
    public class UsuariosController : GsoftController
    {
        private readonly GsoftDbContext _context;
        private readonly ConcurrentDictionary<string, Session> sessions;

        public UsuariosController(GsoftDbContext context, ConcurrentDictionary<string, Session> sessions)
            : base(sessions)
        {
            _context = context;
            this.sessions = sessions;
        }

        /// <summary>
        /// Retorna os dados do usuários autenticado
        /// </summary>
        [HttpGet]
        public IActionResult Get()
        {
            if (!UserAuthenticated)
                return Unauthorized();

            Usuarios usuario = this.Session.usuarioLogado;
            //Usuarios usuario = _context.usuarios.FirstOrDefault();

            usuario.Senha = "";

            if (usuario == null)
            {
                return NoContent();
            }

            return Ok(usuario);
        }

        /// <summary>
        /// Retorna os dados de todos usuários (disponível apenas para admin)
        /// </summary>
        [HttpGet("Todos")]
        public IActionResult GetAll()
        {
            if (!UserAuthenticated)
                return Unauthorized();

            if (Session.usuarioLogado.Perfil != "admin")
                return Unauthorized();

            Usuarios[] usuarios = _context.usuarios.Where(x => x.Empresa == Session.usuarioLogado.Empresa).ToArray();

            if (usuarios == null)
            {
                return NoContent();
            }

            foreach(Usuarios usuario in usuarios)
            {
                usuario.Senha = "";
            }

            return Ok(usuarios);
        }

        /// <summary>
        /// Cadastra um usuário
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]Usuarios user)
        {
            user.ID = 0;

            if(!ValidaUsuario(user))
                return BadRequest("Faltou informação");

            Usuarios usuario = InsertAsync(user).Result;

            if(!ValidaUsuario(usuario))
            {
                return BadRequest("Usuário já cadastrado");
            }

            return Ok(usuario);
        }

        /// <summary>
        /// Altera o usuário cujo ID for passado (não pode alterar o email)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Usuarios value)
        {
            if (!this.UserAuthenticated)
                return Unauthorized();

            if (id != value.ID)
                return BadRequest("Id passado na URL não compatível com o id passado no objeto");
            
            if (!ValidaUsuario(value))
                return BadRequest("Faltou informação");

            var usuario = await UpdateAsync(value);

            if (!ValidaUsuario(usuario))
                return BadRequest("Usuário não encontrado");

            return Ok(usuario);
        }

        /// <summary>
        /// Exclui o usuário cujo ID for passado
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!this.UserAuthenticated)
                return Unauthorized();

            var delete = await DeleteAsync(id);

            if (!delete)
                return BadRequest("Usuário não encotrado");

            return Ok("Sucesso");
        }

        /// <summary>
        /// Retorna o token da sessão do usuário a partir do envio das credenciais corretas
        /// </summary>
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UsuariosLogin user)
        {
            if (user.Email == null || user.Senha == null)
                return BadRequest();

            //senha = CriptoSenha.MD5Senha(senha);
            Usuarios usuarioLogado = await _context.usuarios.FirstOrDefaultAsync(p => p.Email.Equals(user.Email) && p.Senha.Equals(user.Senha));

            if (usuarioLogado == null)
                return BadRequest();

            var sessionExists = this.sessions.Values.FirstOrDefault(x => x.usuarioLogado.ID == usuarioLogado.ID);

            if (sessionExists is null)
            {
                sessionExists = new Session(this.sessions, usuarioLogado);
            }
            else
            {
                sessionExists.UpdateLastRequest();
            }

            //HttpContext.Response.Headers.Add("gsoft-wd-token", sessionExists.Token);

            return Ok(sessionExists.Token);
        }

        [NonAction]
        private bool ValidaUsuario(Usuarios user)
        {
            if(
                user == null ||
                user.Nome == null ||
                user.Email == null ||
                user.Senha == null ||
                user.Perfil == null ||
                user.Empresa == 0
            )
            {
                return false;
            }
            return true;
        }

        [NonAction]
        public async Task<bool> ExistsAsync(Usuarios value)
        {
            return await this._context.usuarios.AnyAsync(x => x.ID == value.ID && x.Email.ToLower() == value.Email.ToLower());
        }

        [NonAction]
        public async Task<bool> ExistsAsync(string email)
        {
            return await this._context.usuarios.AnyAsync(x => x.Email.ToLower() == email.ToLower());
        }

        [NonAction]
        public async Task<Usuarios> InsertAsync(Usuarios value)
        {
            if (await this.ExistsAsync(value.Email))
                return new Usuarios();

            var entityEntry = await this._context.usuarios.AddAsync(value);

            await this._context.SaveChangesAsync();

            this._context.ChangeTracker.Clear();

            return entityEntry.Entity;
        }

        [NonAction]
        public async Task<Usuarios> UpdateAsync(Usuarios value)
        {
            if (!await this.ExistsAsync(value))
                return new Usuarios();

            var entityEntry = this._context.Update(value);

            await this._context.SaveChangesAsync();

            this._context.ChangeTracker.Clear();

            return entityEntry.Entity;
        }

        [NonAction]
        public async Task<bool> DeleteAsync(int value)
        {
            Usuarios usuario = this._context.usuarios.FirstOrDefault(x => x.ID == value);

            if (usuario is null)
                return false;

            this._context.usuarios.Remove(usuario);

            await this._context.SaveChangesAsync();

            this._context.ChangeTracker.Clear();

            return true;
        }

        /*
        [HttpGet("Email/{CNPJ}")]
        public IActionResult EnviarEmail(string CNPJ)
        {
            Empresas empresas = _context.empresas.FirstOrDefault(p => p.CNPJ.Equals(GetCNPJ(CNPJ)));


            if (empresas == null)
            {
                return NoContent();
            }
            empresas.Senha = CriptoSenha.MD5Senha(empresas.CNPJ);
            Email email = _context.email.FirstOrDefault(p => p.ativo);

            EnvioEmail envioEmail = new EnvioEmail(email);
            var a = envioEmail.enviarEmail(empresas);
            return Ok(empresas);
        }

        [HttpPost]
        public IActionResult Login(string cnpj, string senha)
        {
            Empresas empresas = _context.empresas.FirstOrDefault(p => p.CNPJ == GetCNPJ(cnpj));
            if (empresas == null || senha == null)
            {
                return NoContent();
            }

            var Senha = CriptoSenha.MD5Senha(empresas.CNPJ);
            if (empresas.Senha.Equals(senha.ToUpper()))
            {
                return Ok(empresas);
            }
            else
            {
                return NoContent();
            }

        }
        */
    }
}
