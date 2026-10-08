using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Modules.EstructuraAcademica.Dto;
using UniCore.Api.Modules.EstructuraAcademica.Managers;
using UniCore.Api.Modules.EstructuraAcademica.Requests;

namespace UniCore.Api.Modules.EstructuraAcademica.Controllers;

[ApiController, Route("{conexion}/ProgramaModalidad"), Authorize(Policy = "ROL:ADMIN")]
public sealed class ProgramaModalidadController(ProgramaModalidadManager manager) : CatalogoAcademicoController<ProgramaModalidadDto, ProgramaModalidadRequest>(manager) { }
