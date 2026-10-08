using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Modules.EstructuraAcademica.Dto;
using UniCore.Api.Modules.EstructuraAcademica.Managers;
using UniCore.Api.Modules.EstructuraAcademica.Requests;

namespace UniCore.Api.Modules.EstructuraAcademica.Controllers;

[ApiController, Route("{conexion}/Departamentos"), Authorize]
public sealed class DepartamentosController(DepartamentosManager manager) : CatalogoAcademicoController<DepartamentoDto, DepartamentoRequest>(manager) { }
