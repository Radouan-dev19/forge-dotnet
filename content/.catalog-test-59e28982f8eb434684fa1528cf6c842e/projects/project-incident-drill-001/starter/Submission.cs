using System;
using System.Globalization;

public static class Submission
{
    // ---------------------------------------------------------------------------------------
    // À VOUS : les trois méthodes ci-dessous. SimulatedService est fourni plus bas — le
    // modifier ferait échouer vos propres cas, puisque c'est lui qui joue la panne.
    // ---------------------------------------------------------------------------------------

    /// <summary>Rend « minute=M;signal=S » pour un dépassement soutenu sur deux points, sinon « aucun ».</summary>
    public static string DetectStart(string telemetry)
    {
        throw new NotImplementedException();
    }

    /// <summary>Conduit l'incident contre le service simulé et rend « detecte=D;retabli=R », « detecte=D;retabli=jamais » ou « aucun ».</summary>
    public static string RunDrill(string schedule, string action, int lastMinute)
    {
        throw new NotImplementedException();
    }

    /// <summary>Rend « debut=D;duree=Xmin;signal=S;pic=P;action=A » depuis la télémétrie, sinon « aucun ».</summary>
    public static string Postmortem(string telemetry, string action)
    {
        throw new NotImplementedException();
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
