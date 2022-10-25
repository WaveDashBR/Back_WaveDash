﻿using DataAccess;
using Domain;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaBI.API.Services;
using PlataformaBI.API.Servicos;
using PlataformaBI.API.Utils;
using System.Collections.Concurrent;

namespace PlataformaBI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowOrigin")]
    public class EmpresasController : GsoftController
    {
        private readonly GsoftDbContext _context;

        public EmpresasController(GsoftDbContext context, ConcurrentDictionary<string, Session> sessions)
            : base(sessions)
        {
           _context = context;
        }

        /// <summary>
        /// Retorna todos dados de todas empresas
        /// </summary>
        [HttpGet]
        public IActionResult Get()
        {
            if (!UserAuthenticated)
                return Unauthorized();

            IEnumerable<Empresas> empresas = _context.empresas.ToArray();

            if (empresas == null)
            {
                return NoContent();
            }

            return Ok(empresas);
        }
        
        /// <summary>
        /// Retorna os dados do empresas conforme o CNPJ do mesmo
        /// </summary>
        [HttpGet("{CNPJ}")]
        public IActionResult Get(string CNPJ)
        {
            if (!UserAuthenticated)
                return Unauthorized();

            Empresas empresas = _context.empresas.FirstOrDefault(p => p.CNPJ.Equals(Format.GetCNPJ(CNPJ)));

            if (empresas == null)
            {
                return NoContent();
            }

            return Ok(empresas);
        }
    }
}
