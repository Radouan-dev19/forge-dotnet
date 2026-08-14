using ForgeJwtLab.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForgeJwtLab.Controllers;

/// <summary>
/// Ressource protégée par portée : lire et écrire exigent deux droits distincts.
/// </summary>
/// <remarks>
/// La distinction des statuts est le cœur du laboratoire : 401 signifie « je ne sais pas qui
/// vous êtes » — pas de jeton, jeton expiré, signature fausse — et 403 signifie « je sais qui
/// vous êtes, et ce droit vous manque ». Les confondre rend les incidents indiagnosticables.
/// </remarks>
[ApiController]
[Route("orders")]
public sealed class OrdersController : ControllerBase
{
    private static readonly string[] SampleOrders = ["order-1001", "order-1002", "order-1003"];

    [HttpGet]
    [Authorize(Policy = ScopeRequirement.ReadOrders)]
    public ActionResult<IEnumerable<string>> List() => Ok(SampleOrders);

    [HttpPost]
    [Authorize(Policy = ScopeRequirement.WriteOrders)]
    public ActionResult Create() => Created("/orders/order-1004", new { id = "order-1004" });
}
