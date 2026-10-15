namespace RPA.Automation.Recording;

public interface IPositionCaptureService
{
    /// <summary>
    /// Captura as coordenadas (X, Y) do próximo clique do mouse em qualquer lugar da tela.
    /// Retorna null se timeout for atingido sem clique.
    /// </summary>
    Task<(int X, int Y)?> CaptureNextClickAsync(TimeSpan timeout, CancellationToken ct);
}
