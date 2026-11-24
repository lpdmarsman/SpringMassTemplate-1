using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// Check this out we can require components be on a game object!
[RequireComponent(typeof(MeshFilter))]

public class BParticleSimMesh : MonoBehaviour
{
    public struct BSpring
    {
        public float kd;                        // damping coefficient
        public float ks;                        // spring coefficient
        public float restLength;                // rest length of this spring
        public int attachedParticle;            // index of the attached other particle (use me wisely to avoid doubling springs and sprign calculations)
    }

    public struct BContactSpring
    {
        public float kd;                        // damping coefficient
        public float ks;                        // spring coefficient
        public float restLength;                // rest length of this spring (think about this ... may not even be needed o_0
        public Vector3 attachPoint;             // the attached point on the contact surface
    }

    public struct BParticle
    {
        public Vector3 position;                // position information
        public Vector3 velocity;                // velocity information
        public float mass;                      // mass information
        public BContactSpring contactSpring;    // Special spring for contact forces
        public bool attachedToContact;          // is this particle currently attached to a contact (ground plane contact)
        public List<BSpring> attachedSprings;   // all attached springs, as a list in case we want to modify later fast
        public Vector3 currentForces;           // accumulate forces here on each step        
    }

    public struct BPlane
    {
        public Vector3 position;                // plane position
        public Vector3 normal;                  // plane normal
    }

    public float contactSpringKS = 1000.0f;     // contact spring coefficient with default 1000
    public float contactSpringKD = 20.0f;       // contact spring damping coefficient with default 20

    public float defaultSpringKS = 100.0f;      // default spring coefficient with default 100
    public float defaultSpringKD = 1.0f;        // default spring damping coefficient with default 1

    public bool debugRender = false;            // To render or not to render


    /*** 
     * I've given you all of the above to get you started
     * Here you need to publicly provide the:
     * - the ground plane transform (Transform)
     * - handlePlaneCollisions flag (bool)
     * - particle mass (float)
     * - useGravity flag (bool)
     * - gravity value (Vector3)
     * Here you need to privately provide the:
     * - Mesh (Mesh)
     * - array of particles (BParticle[])
     * - the plane (BPlane)
     ***/


    public Transform groundPlaneTransform; // the ground plane transform
    public bool handlePlaneCollisions = true; // handle plane collisions flag
    public float particleMass = 1.0f; // particle mass
    public bool useGravity = true; // use gravity flag
    public Vector3 gravityValue = new Vector3(0.0f, -9.81f, 0.0f); // gravity value

    private Mesh mesh; // the mesh
    private BParticle[] particles; // array of particles
    private BPlane plane; // the plane

    /// <summary>
    /// Init everything
    /// HINT: in particular you should probbaly handle the mesh, init all the particles, and the ground plane
    /// HINT 2: I'd for organization sake put the init particles and plane stuff in respective functions
    /// HINT 3: Note that mesh vertices when accessed from the mesh filter are in local coordinates.
    ///         This script will be on the object with the mesh filter, so you can use the functions
    ///         transform.TransformPoint and transform.InverseTransformPoint accordingly 
    ///         (you need to operate on world coordinates, and render in local)
    /// HINT 4: the idea here is to make a mathematical particle object for each vertex in the mesh, then connect
    ///         each particle to every other particle. Be careful not to double your springs! There is a simple
    ///         inner loop approach you can do such that you attached exactly one spring to each particle pair
    ///         on initialization. Then when updating you need to remember a particular trick about the spring forces
    ///         generated between particles. 
    /// </summary>
    void Start()
    {
        // First lets initialize the mesh and particles
        mesh = GetComponent<MeshFilter>().mesh;
        InitParticles();
        InitPlane();


    }

    void InitParticles()
    {
        Vector3[] vertices = mesh.vertices;
        int verticiesCount = vertices.Length;
        particles = new BParticle[verticiesCount];

        // There should be on particle per vertex, so we are going to loop through all the vercies and create a new particle for each one
        for (int i = 0; i < verticiesCount; i += 1)
        {
            particles[i] = new BParticle
            {
                position = transform.TransformPoint(vertices[i]), // convert local to world coordinates
                velocity = Vector3.zero,
                mass = particleMass,
                contactSpring = new BContactSpring
                {
                    kd = contactSpringKD,
                    ks = contactSpringKS,
                    restLength = 0.0f, // Probably 0? Idk if we actually need to change/add this
                    attachPoint = Vector3.zero
                },
                attachedToContact = false,
                attachedSprings = new List<BSpring>(),
                currentForces = Vector3.zero
            };
        }

        for (int i = 0; i < verticiesCount; i += 1)
        {
            for (int j = i + 1; j < verticiesCount; j += 1)
            {
                float restLength = Vector3.Distance(particles[i].position, particles[j].position); // Should already be in world coordinates
                BSpring spring = new BSpring
                {
                    kd = defaultSpringKD,
                    ks = defaultSpringKS,
                    restLength = restLength,
                    attachedParticle = j
                };
                particles[i].attachedSprings.Add(spring);
            }
        }
    }
    void InitPlane()
    {
        plane.position = groundPlaneTransform.position;
        plane.normal = groundPlaneTransform.up; // Assuming the plane's normal is its up vector
    }


    /*** BIG HINT: My solution code has as least the following functions
     * InitParticles()
     * InitPlane()
     * UpdateMesh() (remember the hint above regarding global and local coords)
     * ResetParticleForces()
     * ...
     ***/

    private Vector3 ApplyGravity(Vector3 currentForce)
    {
        return currentForce + (gravityValue * particleMass); // Force = mass * acceleration, and gravityValue is acceleration
    }


    /// <summary>
    /// Draw a frame with some helper debug render code
    /// </summary>
    public void Update()
    {
        // Debug.Log("Updating frame");
        // This will work if you have a correctly made particles array
        if (debugRender)
        {
            int particleCount = particles.Length;
            // Debug.Log("Particle Positions:");
            for (int i = 0; i < particleCount; i++)
            {
                // Debug.DrawLine(particles[i].position, particles[i].position + particles[i].currentForces, Color.blue);

                int springCount = particles[i].attachedSprings.Count;
                for (int j = 0; j < springCount; j++)
                {
                    Debug.DrawLine(particles[i].position, particles[particles[i].attachedSprings[j].attachedParticle].position, Color.red);
                }
                // Debug.Log($"Particle {i}: Position = {particles[i].position}, Velocity = {particles[i].velocity}, Current Forces = {particles[i].currentForces}");
            }
        }
    }

    public void FixedUpdate()
    {
        // This is where you will do your physics update
        // HINT: You will need to reset forces, apply forces, integrate, and update the mesh
        // Not super sure if order matters here, but can't hurt to do it in this order I assume?
        ResetParticleForces();
        ApplyForces();
        Integrate(Time.fixedDeltaTime);
        UpdateMesh();
    }

    public void ResetParticleForces()
    {
        int particleCount = particles.Length;
        for (int i = 0; i < particleCount; i += 1)
        {
            // Go through all the particles and apply gravity to their current force
            particles[i].currentForces = new Vector3(0, 0, 0);
        }
    }
    public void ApplyForces()
    {
        // Here we are applying fores to the particles
        // Debug.Log("Applying forces");
        int particleCount = particles.Length;
        // Debug.Log("Particle Positions:");
        for (int i = 0; i < particleCount; i += 1)
        {
            if (useGravity)
            {
                // Go through all the particles and apply gravity to their current force
                particles[i].currentForces = ApplyGravity(particles[i].currentForces);
            } 

            if (handlePlaneCollisions)
            {
                // Check for collision with the plane
                float distanceToPlane = Vector3.Dot(particles[i].position - plane.position, plane.normal);
                if (distanceToPlane < 0)
                {
                    // Assuming this is how we want it computed, not sure what it does?
                    if (particles[i].attachedToContact == false)
                    {
                        particles[i].attachedToContact = true;
                        particles[i].contactSpring.attachPoint = particles[i].position - (distanceToPlane * plane.normal); // This is never used, but its in the struct?
                    }
                    // Particle is below the plane, apply contact spring force
                    Vector3 contactForce = (-particles[i].contactSpring.ks * (Vector3.Dot((particles[i].position - particles[i].contactSpring.attachPoint), plane.normal) * plane.normal)) - particles[i].contactSpring.kd * particles[i].velocity;
                    particles[i].currentForces += contactForce;
                } else
                {
                    // Particle is above the plane, no contact force
                    particles[i].attachedToContact = false;
                }
            }

            if (true)
            {
                List<BSpring> attachedSprings = particles[i].attachedSprings;
                // Go through all the attached springs and apply spring forces now
                foreach (BSpring spring in attachedSprings)
                {
                    Vector3 otherParticlePosition = particles[spring.attachedParticle].position;
                    float springVectorLength = (particles[i].position - otherParticlePosition).magnitude;
                    Vector3 normalSpringPositionVector = (particles[i].position - otherParticlePosition).normalized;
                    Vector3 otherParticleVelocity = particles[spring.attachedParticle].velocity;
                    Vector3 springVelocityVector = (particles[i].velocity - otherParticleVelocity);

                    Vector3 springForces = (spring.ks * ((spring.restLength - springVectorLength) * normalSpringPositionVector)) - 
                        (spring.kd * (Vector3.Dot(springVelocityVector, normalSpringPositionVector) * normalSpringPositionVector));

                    particles[i].currentForces += springForces;
                    particles[spring.attachedParticle].currentForces -= springForces; // Gotta apply the forces to the other particle too
                }
            }
        }
    }

    public void Integrate(float deltaTime)
    {
        // Here we are integrating the particles using Symplectic Euler for second order systems following the slides (Slide 14).
        // Debug.Log("Integrating particles");
        int particleCount = particles.Length;
        for (int i = 0; i < particleCount; i += 1)
        {
            // Symplectic Euler
            particles[i].velocity += deltaTime * (particles[i].currentForces / particles[i].mass) ;
            particles[i].position += deltaTime * particles[i].velocity;
        }
    }
    private void UpdateMesh()
    {
        // Debug.Log("Updating mesh");
        int particleCount = particles.Length;
        Vector3[] vertices = new Vector3[particleCount];
        for (int i = 0; i < particleCount; i += 1)
        {
            // Debug.Log("Updating particle " + i);
            vertices[i] = transform.InverseTransformPoint(particles[i].position); // convert world to local coordinates
        }
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}