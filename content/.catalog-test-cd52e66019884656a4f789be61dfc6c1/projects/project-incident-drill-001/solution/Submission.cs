using System;
using System.Globalization;
using System.Linq;

public static class Submission
{
    public static string DetectStart(string telemetry)
    {
        (int Minute, int Latency, int Errors)[] points = ParseTelemetry(telemetry);
        for (int index = 0; index < points.Length - 1; index++)
        {
            // Un pic isolé n'est pas un incident : le dépassement doit tenir deux points de suite.
            if (IsBreach(points[index]) && IsBreach(points[index + 1]))
            {
                string signal = points[index].Errors >= 20 ? "erreurs" : "latence";
                return $"minute={points[index].Minute};signal={signal}";
            }
        }

        return "aucun";
    }

    public static string RunDrill(string schedule, string action, int lastMinute)
    {
        var service = new SimulatedService(schedule);
        int? detectedAt = null;
        int? recoveredAt = null;

        for (int minute = 0; minute <= lastMinute; minute++)
        {
            (int, int Latency, int Errors) point = ParsePoint(service.Signal(minute));
            bool breach = point.Latency >= 800 || point.Errors >= 20;

            if (detectedAt is null && breach)
            {
                // On agit sur un seul point : pour intervenir, on n'attend pas une confirmation.
                detectedAt = minute;
                service.Apply(action, minute);
                continue;
            }

            if (detectedAt is not null && recoveredAt is null && !breach)
            {
                // Le rétablissement se constate sur les signaux relus, jamais sur l'action posée.
                recoveredAt = minute;
            }
        }

        if (detectedAt is null)
        {
            return "aucun";
        }

        string recovery = recoveredAt is null ? "jamais" : recoveredAt.Value.ToString(CultureInfo.InvariantCulture);
        return $"detecte={detectedAt};retabli={recovery}";
    }

    public static string Postmortem(string telemetry, string action)
    {
        (int Minute, int Latency, int Errors)[] points = ParseTelemetry(telemetry);
        var breaches = points.Where(IsBreach).ToArray();
        if (breaches.Length == 0)
        {
            return "aucun";
        }

        int start = breaches[0].Minute;
        // La fenêtre d'impact va du premier au dernier dépassement : une accalmie au milieu
        // ne raccourcit pas un incident, elle le rend seulement intermittent.
        int duration = breaches[^1].Minute - start + 1;
        string signal = breaches[0].Errors >= 20 ? "erreurs" : "latence";
        int peak = signal == "erreurs"
            ? breaches.Max(point => point.Errors)
            : breaches.Max(point => point.Latency);
        return $"debut={start};duree={duration}min;signal={signal};pic={peak};action={action}";
    }

    private static bool IsBreach((int Minute, int Latency, int Errors) point) =>
        point.Latency >= 800 || point.Errors >= 20;

    private static (int Minute, int Latency, int Errors)[] ParseTelemetry(string telemetry) =>
        telemetry.Split(';').Select(ParsePoint).ToArray();

    private static (int Minute, int Latency, int Errors) ParsePoint(string point)
    {
        string[] parts = point.Split(':');
        return (
            int.Parse(parts[0], CultureInfo.InvariantCulture),
            int.Parse(parts[1], CultureInfo.InvariantCulture),
            int.Parse(parts[2], CultureInfo.InvariantCulture));
    }

    // ======================= FOURNI — ne pas modifier =======================

    /// <summary>
    /// Service simulé : une panne de nature et de minute connues de lui seul, des signaux
    /// mesurables, et une atténuation dont l'effet devient visible deux minutes après.
    /// </summary>
    public sealed class SimulatedService
    {
        private readonly string _faultKind;
        private readonly int _faultMinute;
        private int? _healthyFrom;

        public SimulatedService(string schedule)
        {
            string[] parts = schedule.Split('@');
            _faultKind = parts[0];
            _faultMinute = int.Parse(parts[1], CultureInfo.InvariantCulture);
        }

        /// <summary>Rend la mesure « minute:latenceMs:erreurs » de la minute demandée.</summary>
        public string Signal(int minute)
        {
            bool faulty = minute >= _faultMinute && (_healthyFrom is null || minute < _healthyFrom);
            if (!faulty)
            {
                return $"{minute}:120:0";
            }

            return _faultKind == "deploiement" ? $"{minute}:180:25" : $"{minute}:950:0";
        }

        /// <summary>Tente une atténuation ; seule l'action adaptée à la panne rétablit le service.</summary>
        public void Apply(string action, int minute)
        {
            bool matches = (_faultKind == "deploiement" && action == "retour-arriere")
                || (_faultKind == "charge" && action == "montee-en-charge");
            if (matches && _healthyFrom is null)
            {
                _healthyFrom = minute + 2;
            }
        }
    }
}
