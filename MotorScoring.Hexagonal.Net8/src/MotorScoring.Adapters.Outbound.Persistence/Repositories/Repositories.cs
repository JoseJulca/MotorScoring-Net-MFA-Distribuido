using Microsoft.EntityFrameworkCore;
using MotorScoring.Adapters.Outbound.Persistence.Context;
using MotorScoring.Adapters.Outbound.Persistence.Mappers;
using MotorScoring.Adapters.Outbound.Persistence.Models;
using MotorScoring.Application.Ports.Out;
using MotorScoring.Domain.Entities;
using MotorScoring.Domain.ValueObjects;
namespace MotorScoring.Adapters.Outbound.Persistence.Repositories;

public sealed class SolicitanteRepository(MotorScoringDbContext db) : ISolicitanteRepository
{
    public async Task<Solicitante?> BuscarPorDocumentoAsync(NumeroDocumento d, CancellationToken ct) => (await db.Solicitantes.AsNoTracking().FirstOrDefaultAsync(x => x.TipoDocumento == d.Tipo.ToString() && x.NumeroDocumento == d.Numero, ct))?.ToDomain();
    public async Task<Solicitante?> BuscarPorIdAsync(Guid id, CancellationToken ct) => (await db.Solicitantes.AsNoTracking().FirstOrDefaultAsync(x => x.IdSolicitante == id, ct))?.ToDomain();
    public async Task GuardarAsync(Solicitante s, CancellationToken ct)
    {
        var current = await db.Solicitantes.FindAsync([s.Id], ct);
        var m = s.ToModel();
        if (current is null) await db.Solicitantes.AddAsync(m, ct);
        else db.Entry(current).CurrentValues.SetValues(m);
    }
}
public sealed class SolicitudCreditoRepository(MotorScoringDbContext db) : ISolicitudCreditoRepository
{
    public Task<bool> ExistePorIdentificadorExternoAsync(IdentificadorExterno id, CancellationToken ct) => db.Solicitudes.AnyAsync(x => x.IdentificadorExterno == id.Valor, ct);
    public async Task<SolicitudCredito?> BuscarPorIdAsync(Guid id, CancellationToken ct) => (await db.Solicitudes.AsNoTracking().FirstOrDefaultAsync(x => x.IdSolicitud == id, ct))?.ToDomain();
    public async Task GuardarAsync(SolicitudCredito s, CancellationToken ct)
    {
        var current = await db.Solicitudes.FindAsync([s.Id], ct);
        var m = s.ToModel();
        if (current is null) await db.Solicitudes.AddAsync(m, ct);
        else db.Entry(current).CurrentValues.SetValues(m);
    }
}
public sealed class ProductoCrediticioRepository(MotorScoringDbContext db) : IProductoCrediticioRepository
{
    public async Task<ProductoCrediticio?> BuscarPorCodigoAsync(string codigo, CancellationToken ct) => (await db.Productos.AsNoTracking().FirstOrDefaultAsync(x => x.Codigo == codigo, ct))?.ToDomain();
    public async Task<ProductoCrediticio?> BuscarPorIdAsync(Guid id, CancellationToken ct) => (await db.Productos.AsNoTracking().FirstOrDefaultAsync(x => x.IdProducto == id, ct))?.ToDomain();
}
public sealed class ModeloScoringRepository(MotorScoringDbContext db) : IModeloScoringRepository
{
    public async Task<ModeloScoring?> BuscarCompletoPorIdAsync(Guid id, CancellationToken ct)
    {
        var m = await db.Modelos.AsNoTracking().Include(x => x.Versiones).ThenInclude(x => x.Factores).ThenInclude(x => x.Reglas).FirstOrDefaultAsync(x => x.IdModelo == id, ct);
        return m?.ToDomain();
    }
}
public sealed class EvaluacionCrediticiaRepository(MotorScoringDbContext db) : IEvaluacionCrediticiaRepository
{
    public Task<bool> ExistePorSolicitudYVersionAsync(Guid s, Guid v, CancellationToken ct) => db.Evaluaciones.AnyAsync(x => x.IdSolicitud == s && x.IdVersionModelo == v, ct);
    public async Task GuardarAsync(EvaluacionCrediticia e, CancellationToken ct)
    {
        var model = new EvaluacionCrediticiaModel
        {
            IdEvaluacion = e.Id,
            IdSolicitud = e.IdSolicitud,
            IdVersionModelo = e.IdVersionModelo,
            FechaEvaluacion = e.FechaEvaluacion,
            PuntajeTotal = e.PuntajeTotal.Valor,
            Resultado = e.Resultado.ToString(),
            Estado = e.Estado.ToString(),
            ResultadosFactor = e.ResultadosFactor.Select(r => new ResultadoFactorModel
            {
                IdResultadoFactor = Guid.NewGuid(),
                IdEvaluacion = e.Id,
                IdFactor = r.IdFactor,
                CodigoFactor = r.CodigoFactor,
                ValorEvaluado = r.ValorEvaluado,
                PesoAplicado = r.PesoAplicado,
                PuntajeBase = r.PuntajeBase,
                PuntajeObtenido = r.PuntajeObtenido,
                ReglaAplicada = r.ReglaAplicada,
                Observacion = r.Observacion,
                ReglaExcluyente = r.ReglaExcluyente,
                ResultadoExcluyente = r.ResultadoExcluyente?.ToString()
            }).ToList(),
            ResultadoScoring = new ResultadoScoringModel
            {
                IdResultadoScoring = Guid.NewGuid(),
                IdEvaluacion = e.Id,
                PuntajeTotal = e.PuntajeTotal.Valor,
                Resultado = e.Resultado.ToString(),
                FechaResultado = e.FechaEvaluacion
            }
        };
        await db.Evaluaciones.AddAsync(model, ct);
    }
}
public sealed class UnitOfWork(MotorScoringDbContext db) : IUnitOfWork
{
    public Task<int> CommitAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}