using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Modules.EstructuraAcademica.Dto;
using UniCore.Api.Modules.EstructuraAcademica.Managers;
using UniCore.Api.Modules.EstructuraAcademica.Requests;

namespace UniCore.Api.Modules.EstructuraAcademica.Controllers;

[ApiController, Route("{conexion}/ProgramaModalidad"), Authorize]
public sealed class ProgramaModalidadController : CatalogoAcademicoController<ProgramaModalidadDto, ProgramaModalidadRequest>
{
    public ProgramaModalidadController(ProgramaModalidadManager manager)
        : base(manager)
    {
    }
}
