using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Threading;

namespace SolarSystemSimulator
{
    class Simulator
    {
        private List<CelestialBody> bodies = new List<CelestialBody>();

        public volatile bool isRunning = false;

        private volatile int iterations = 0;
        private float stepLength = 20;
        private bool isTimedSimulation = false;
        private double simulationTime = 0;
        private double targetSpeed = 1;
        private bool hasSpeedTarget = true;

        private const double gravConst = 6.67408E-17;

        #region Properties
        public double SimulationTime
        {
            get { return simulationTime; }
            set { if (!isRunning && value >= 0) simulationTime = value; }
        }

        public float StepLength
        {
            get { return stepLength; }
            set { if (!isRunning && value > 0) stepLength = value; }
        }

        public double TargetSpeed
        {
            get { return targetSpeed; }
            set { if (value > 0) targetSpeed = value; }
        }

        public bool HasSpeedTarget
        {
            get { return hasSpeedTarget; }
            set { if (!isRunning) hasSpeedTarget = value; }
        }
        #endregion

        #region Body-related methods
        public List<CelestialBody> GetBodies()
        {
            List<CelestialBody> result = new List<CelestialBody>(bodies.Count);
            foreach (CelestialBody body in bodies)
            {
                result.Add(body.GetCopy());
            }
            return result;
        }
        #endregion

        #region The Runge-Kutta method
        private void Rk4Integrate(SimpleBody[] sBodies, float step)
        {
            // Intermediate states
            SimpleBody[][] s = new SimpleBody[4][];
            for (int i = 0; i < 4; i++) s[i] = new SimpleBody[sBodies.Length];
            double dt = 0.0;

            for (int state = 0; state < 4; state++)
            {
                if (state == 1 || state == 3) dt += 0.5 * step;

                Vector3D[] position = new Vector3D[sBodies.Length], // Vector arrays where new state is constructed
                                 dv = new Vector3D[sBodies.Length];

                for (int i = 0; i < sBodies.Length; i++)
                {
                    position[i] = dt == 0.0 ? sBodies[i].position : sBodies[i].position + dt / 1000.0 * s[state - 1][i].velocity;
                }

                for (int i = 0; i < sBodies.Length - 1; i++) // Set acceleration vectors
                {
                    for (int j = i + 1; j < sBodies.Length; j++)
                    {
                        Vector3D distance = position[j] - position[i];
                        double gravityModifier = gravConst / (distance.LengthSquared * distance.Length);

                        dv[i] = dv[i] + gravityModifier * sBodies[j].mass * distance;
                        dv[j] = dv[j] - gravityModifier * sBodies[i].mass * distance;
                    }
                }

                // Create a new state using the constructed vector arrays
                for (int i = 0; i < sBodies.Length; i++) s[state][i] = new SimpleBody(sBodies[i].mass,
                    dt == 0 ? sBodies[i].velocity : sBodies[i].velocity + dt * s[state - 1][i].dv,
                    position[i],
                    dv[i]);
            }

            // Apply integration steps to original state
            for (int i = 0; i < sBodies.Length; i++)
            {
                Vector3D dxdt = (s[0][i].velocity + 2 * (s[1][i].velocity + s[2][i].velocity) + s[3][i].velocity) / 6.0;
                Vector3D dvdt = (s[0][i].dv + 2 * (s[1][i].dv + s[2][i].dv) + s[3][i].dv) / 6.0;

                sBodies[i] = new SimpleBody(sBodies[i].mass,
                    sBodies[i].velocity + step * dvdt,
                    sBodies[i].position + step / 1000.0 * dxdt);
            }

        }
        #endregion

        #region Simulation
        public void StartSimulation(List<CelestialBody> bodiesToSimulate)
        {
            bodies = new List<CelestialBody>(bodiesToSimulate.Count); // Get a private copy of the bodies inserted to avoid alteration from other sources.
            foreach (CelestialBody body in bodiesToSimulate) bodies.Add(body.GetCopy());

            ProcessCollisions();
            SimpleBody[] sBodies = new SimpleBody[bodies.Count];
            for (int i = 0; i < bodies.Count; i++) sBodies[i] = bodies[i].ToSimple();

            isTimedSimulation = simulationTime == 0.0 ? false : true;
            if (hasSpeedTarget) stepLength = 0.01F;

            isRunning = true;

            while (isRunning && sBodies.Length > 0)
            {
                float step = stepLength; // Use a local copy to ensure it won't be changed by other thread during iteration step.
                Rk4Integrate(sBodies, step);

                for (int i = 0; i < bodies.Count; i++) // Update originals with new values.
                {
                    bodies[i].position = sBodies[i].position;
                    bodies[i].velocity = sBodies[i].velocity;
                }

                ProcessCollisions();
                if (bodies.Count != sBodies.Length)
                {
                    sBodies = new SimpleBody[bodies.Count];
                    for (int i = 0; i < bodies.Count; i++) sBodies[i] = bodies[i].ToSimple();
                }

                if (isTimedSimulation)
                {
                    simulationTime -= step / 86400.0;
                    if (simulationTime <= 0.0)
                    {
                        isRunning = false;
                        simulationTime = 0.0;
                    }
                }
                else
                {
                    simulationTime += step / 86400.0;
                }

                Interlocked.Increment(ref iterations); // Used for speed calculations.
            }

            if (!isTimedSimulation) SimulationTime = 0.0;
        }

        public void MonitorTime() // Async method used to control simulation speed.
        {
            Stopwatch timer = Stopwatch.StartNew();
            Thread.Sleep(100);
            while (isRunning)
            {
                if (iterations > 0)
                {
                    int i = Interlocked.Exchange(ref iterations, 0);
                    if (hasSpeedTarget) stepLength = (float)(targetSpeed * timer.Elapsed.TotalSeconds * 86400 / i);
                    else targetSpeed = stepLength * i / (timer.Elapsed.TotalSeconds * 86400);
                    timer.Restart();
                    Thread.Sleep(500);
                }   
            }
        }

        private void ProcessCollisions()
        {
            int i = 0, j = 0;
            while (i < bodies.Count - 1)
            {
                j = i + 1;
                do
                {
                    if ((bodies[i].position - bodies[j].position).Length < bodies[i].radius + bodies[j].radius)
                    {
                        bool firstHeavier = bodies[i].mass >= bodies[j].mass;
                        CelestialBody heavy = firstHeavier ? bodies[i] : bodies[j];
                        CelestialBody light = firstHeavier ? bodies[j] : bodies[i];
                        double combinedMass = heavy.mass + light.mass;

                        heavy.velocity = (heavy.velocity * heavy.mass + light.velocity * light.mass) / combinedMass;
                        heavy.position = heavy.position + (light.position - heavy.position) * light.mass / combinedMass;

                        heavy.radius = Math.Pow(combinedMass / heavy.mass, 1.0 / 3.0) * heavy.radius;
                        heavy.mass = combinedMass;

                        bodies.RemoveAt(firstHeavier ? j : i);

                        i = -1;
                        j = bodies.Count;
                    }
                    else
                    {
                        j++;
                    }
                } while (j < bodies.Count);
                i++;
            }
        }
        #endregion
    }
}
