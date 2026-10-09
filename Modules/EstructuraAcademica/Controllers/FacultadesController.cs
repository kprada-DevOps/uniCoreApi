using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCore.Api.Modules.EstructuraAcademica.Dto;
using UniCore.Api.Modules.EstructuraAcademica.Managers;
using UniCore.Api.Modules.EstructuraAcademica.Requests;

namespace UniCore.Api.Modules.EstructuraAcademica.Controllers;

[ApiController, Route("{conexion}/Facultades"), Authorize]
public sealed class FacultadesController : CatalogoAcademicoController<FacultadDto, FacultadRequest>
{
    public FacultadesController(FacultadesManager manager)
        : base(manager)
    {
    }
}
