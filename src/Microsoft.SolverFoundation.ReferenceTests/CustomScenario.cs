using System.Diagnostics;
using System.Linq;
using System;
using Microsoft.SolverFoundation.Services;

namespace Microsoft.SolverFoundation.ReferenceTests
{
    public class CustomScenario
    {
        private SolverContext _context;
        private Solution _solution;

        public void Create()
        {
            int timeSteps = 24; // Optimierung für 24 Stunden
            double batteryCapacity = 36.0; // kWh
            double maxChargePower = 15.0; // kW
            double maxDischargePower = 15.0; // kW
            double etaCharge = 1; //0.9; // Wirkungsgrad Laden
            double etaDischarge = 1; //0.9; // Wirkungsgrad Entladen

            // Beispielhafte Strompreise, PV-Erträge und Verbrauchsdaten
            double[] gridPrices = { 0.30, 0.28, 0.25, 0.22, 0.20, 0.18, 0.15, 0.14, 0.16, 0.18, 0.22, 0.26, 0.30, 0.32, 0.34, 0.35, 0.36, 0.38, 0.40, 0.39, 0.35, 0.32, 0.30, 0.28 };

            double[] pvGeneration = { 0, 0, 0, 0, 0, 0.5, 1.5, 3, 5, 6, 5, 4, 3, 2, 1.5, 1, 0.5, 0, 0, 0, 0, 0, 0, 0 };
            double[] consumption = { 0.8, 0.7, 0.6, 0.5, 0.4, 0.6, 0.8, 1.2, 2, 2.5, 2.8, 3, 3.2, 3, 2.8, 2.5, 2, 1.8, 1.5, 1.2, 1, 0.9, 0.8, 0.7 };

            var sumPv = pvGeneration.Sum();
            var sumConsumption = consumption.Sum();
            var diff = sumPv - sumConsumption;
            Console.WriteLine($"PV-Erzeugung: {sumPv} kWh, Verbrauch: {sumConsumption} kWh, Differenz: {diff} kWh");

            var timestamps = Enumerable.Range(0, timeSteps).ToList();

            _context = SolverContext.GetContext();
            var model = _context.CreateModel();

            // Entscheidungsvariablen
            var charge = new Decision[timeSteps];
            var discharge = new Decision[timeSteps];
            var soc = new Decision[timeSteps];
            for (int t = 0; t < timeSteps; t++)
            {
                soc[t] = new Decision(Domain.RealRange(0, batteryCapacity), $"soc_______{timestamps[t]}");
                charge[t] = new Decision(Domain.RealRange(0, maxChargePower), $"charge____{timestamps[t]}");
                discharge[t] = new Decision(Domain.RealRange(0, maxDischargePower), $"discharge_{timestamps[t]}");

                //charge[t] = new Decision(Domain.RealNonnegative,            $"charge_____{timestamps[t]}");
                //discharge[t] = new Decision(Domain.RealNonnegative,          $"discharge_{timestamps[t]}");            
                model.AddDecisions(soc[t], charge[t], discharge[t]);
            }

            // Zielfunktion: Minimierung der Stromkosten
            Term costTerm = 0;  // Startwert als 0
            Term chargeDischargeTerm = 0;  // Startwert als 0
            for (int t = 0; t < timeSteps; t++)
            {
                // Füge für jede Stunde die Kosten zur Zielfunktion hinzu
                costTerm += gridPrices[t] * (consumption[t] - pvGeneration[t] + charge[t] - discharge[t]);

                chargeDischargeTerm += charge[t] + discharge[t];
            }
            model.AddGoal("MinimizeCost", GoalKind.Minimize, costTerm);
            model.AddGoal("MinimizeBatteryCharging", GoalKind.Minimize, chargeDischargeTerm);

            // Nebenbedingungen
            for (int t = 0; t < timeSteps; t++)
            {
                if (t == 0)
                {
                    model.AddConstraint($"soc_init_{timestamps[t]}", soc[t] == 10.0); // Start mit 50% Batterieladung => batteryCapacity/2
                }
                else
                {
                    model.AddConstraint($"soc_balance_{timestamps[t]}",
                        soc[t] == soc[t - 1]
                            + etaCharge * charge[t - 1]
                            - discharge[t - 1] * etaDischarge);

                    // begrenze Lade- und Entladeleistung der Batterie, kann mehr entladen als drin ist oder mehr laden als rein passt
                    //model.AddConstraint($"charge_limit_{timestamps[t]}", charge[t] <= maxChargePower);
                }

                // begrenze Lade- und Entladeleistung der Batterie
                //model.AddConstraint($"charge_limit_{timestamps[t]}", charge[t] <= maxChargePower);
                //model.AddConstraint($"discharge_limit_{timestamps[t]}", discharge[t] <= maxDischargePower);

                // Nebenbedingung: Der Entladevorgang kann nicht mehr Strom entladen als der Ladezustand der Batterie es zulässt
                model.AddConstraint($"discharge_limit_by_soc_{timestamps[t]}", discharge[t] <= soc[t]);
                // Nebenbedingung: Der Entladevorgang kann nicht mehr Strom entladen als verwendet wird
                model.AddConstraint($"discharge_limit_by_consumption_{timestamps[t]}", discharge[t] <= consumption[t]);

                // Nebenbedingung: Strom muss verbraucht werden
                //model.AddConstraint($"energy_has_to_be_used_{timestamps[t]}", (consumption[t] - pvGeneration[t] + charge[t] - discharge[t]) < 0.1 );
            }
        }

        public Solution Solve()
        {
            _solution = _context.Solve();
            return _solution;
        }

        public void Dump()
        {
            Report report = _solution.GetReport();
            Console.WriteLine(report);
            Debug.WriteLine(report);
        }
    }
}