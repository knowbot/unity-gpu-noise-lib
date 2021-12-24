using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    public class NoiseManager : Singleton<NoiseManager>, ISubject
    {
        public enum Function
        {
            Perlin2D,
            Perlin3D,
            Perlin4D,
            Simplex2D,
            Simplex3D,
            Simplex4D
        }

        public Function function;
        [SerializeField] private float multiplier = 1;
        [SerializeField] private Vector4 offset = Vector4.zero;
        [SerializeField] private int octaves = 4;
        [SerializeField] [Range(0, 16)] private float lacunarity = 2;
        [SerializeField] [Range(0, 1)] private float gain = 0.5f;
        [SerializeField] [Range(0, 16)] private float amplitude = 1.5f;
        [SerializeField] [Range(0, 16)] private float frequency = 1;
        [SerializeField] [Range(0, 100)] private float scale = 1;
        [SerializeField] [Range(0.1f, 10)] private float exponent = 1;

        // Implement ISubject interface
        public List<IObserver> Observers { get; } = new List<IObserver>();
        public void Attach(IObserver observer) { Observers.Add(observer); } 
        public void Detach(IObserver observer) { Observers.Remove(observer); }
        public void NotifyAll()
        {
            foreach (IObserver o in Observers)
                o.Receive(this);
        }

        private void OnValidate()
        {
            NotifyAll();
        }

        public float Multiplier
        {
            get => multiplier;
            set
            {
                NotifyAll();
                multiplier = value;
            }
        }

        public Vector4 Offset
        {
            get => offset;
            set
            {
                NotifyAll();
                offset = value;
            }
        }

        public int Octaves
        {
            get => octaves;
            set
            {
                NotifyAll();
                octaves = value;
            }
        }

        public float Lacunarity
        {
            get => lacunarity;
            set
            {
                NotifyAll();
                lacunarity = value;
            }
        }

        public float Gain
        {
            get => gain;
            set
            {
                NotifyAll();
                gain = value;
            }
        }

        public float Amplitude
        {
            get => amplitude;
            set
            {
                NotifyAll();
                amplitude = value;
            }
        }

        public float Frequency
        {
            get => frequency;
            set
            {
                NotifyAll();
                frequency = value;
            }
        }

        public float Scale
        {
            get => scale;
            set
            {
                NotifyAll();
                scale = value;
            }
        }

        public float Exponent
        {
            get => exponent;
            set
            {
                NotifyAll();
                exponent = value;
            }
        }
    }
}
