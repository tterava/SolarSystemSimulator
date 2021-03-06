﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Runtime.Serialization;

namespace SolarSystemSimulator
{
    class CelestialBody
    {
        public String name;
        public double mass;
        public Vector3D position;
        public Vector3D velocity;
        public double radius;
        public double magnification;
        public SolidColorBrush color;
        public readonly long uniqueID;

        private static ObjectIDGenerator idGen = new ObjectIDGenerator();

        public CelestialBody(String name, double mass, Vector3D position, Vector3D velocity, double radius, double magnification, SolidColorBrush color)
        {
            this.name = name;
            this.mass = mass;
            this.position = position;
            this.velocity = velocity;
            this.radius = radius;
            this.magnification = magnification;
            this.color = color;

            bool firstTime;
            this.uniqueID = idGen.GetId(this, out firstTime);
        }

        private CelestialBody(string name, double mass, Vector3D position, Vector3D velocity, double radius, double magnification, SolidColorBrush color, long uniqueID) : this(name, mass, position, velocity, radius, magnification, color)
        {
            this.uniqueID = uniqueID;
        }

        public CelestialBody GetCopy()
        {
            return new CelestialBody(name, mass, position, velocity, radius, magnification, color, uniqueID);
        }

        public SimpleBody ToSimple()
        {
            return new SimpleBody(mass, velocity, position);
        }
    }

    struct SimpleBody
    {
        public double mass;
        public Vector3D velocity;
        public Vector3D position;
        public Vector3D dv; 

        public SimpleBody(double mass, Vector3D velocity, Vector3D position, Vector3D dv)
        {
            this.mass = mass;
            this.velocity = velocity;
            this.position = position;
            this.dv = dv;
        }

        public SimpleBody(double mass, Vector3D velocity, Vector3D position)
        {
            this.mass = mass;
            this.velocity = velocity;
            this.position = position;
            this.dv = new Vector3D(0,0,0);
        }
    }
}
