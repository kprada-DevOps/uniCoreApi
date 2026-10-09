using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Modules.EstructuraAcademica.Dto;
using UniCore.Api.Modules.EstructuraAcademica.Managers;
using UniCore.Api.Modules.EstructuraAcademica.Requests;

namespace UniCore.Api.Modules.EstructuraAcademica.Controllers;

[ApiController, Route("{conexion}/Programas"), Authorize]
public sealed class ProgramasController : CatalogoAcademicoController<ProgramaDto, ProgramaRequest>
{
    public ProgramasController(ProgramasManager manager)
        : base(manager)
    {
    }
}
