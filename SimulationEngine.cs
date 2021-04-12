/*
"Disofenix 117"
Diego Esteban Suarez C.		1201689
Universidad Militar Nueva Granada
2020
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using UnityEngine;
using Debug = UnityEngine.Debug;
public struct vectores
{
    public Vector3 pos;
    public Vector3 vel;
}
public struct Particle
{
    //Propiedades de la particula
    public vectores Fase, DFase;//estados de fase 
    public float masa;          //masa 
    public float Radio;         //Radio 
    public Vector3 fuerza;      //fuerza
    public Vector3 Impulso;     //Impulso
    public Vector3 DistEnEsf1;  //distancias de acercamiento con las esferas
    public Vector3 DistEnEsf2;  //distancias de acercamiento con las esferas
    public bool[] hueco;        //condicion si pasa superficie
}
public struct spring
{
    public float longitud;      //Longitud
    public Vector3 distancia;   //distancia del resorte con respecto a sus anclajes
    public Vector3 dX;          //vector direccion particula-anclaje
    public float dL;            //longitud de deformacion del resorte
    public float Ke;            //coheficiente de restitucion
    public float Kb;            //coheficiente de amortiguamiento
    


}

public class SimulationEngine : MonoBehaviour
{
    public Particle[] Particula;
    public spring Resorte;
    public float mStep;           //Paso del método numérico (h)

    private GameObject[] mViewEsphere;        //Esfera para visualizar el movimiento numérico de la partícula
    private GameObject[] mViewEscenario;       //Escenario a Visualizar
    private float Ro, Ri;                      //Radios de las esferas a colisionar
    private int cantEsferas;                   //Pariculas

    private float mMass;                    //Masa de la partícula                
    private float mTime;                    //Tiempo de la simulación
    private float R;                        //Radio de la particula
    private float s;                    //friccion aire
    private float miu;                    //friccion suelo

    private vectores k1, k2, k3, k4;

    private float gravedad;
    private Vector3 Gravedad;
    private Vector3 Origen;

    private float Epsilon;

    private Vector3 A;                      //posicion del Anclaje
    private float Ang;                      //abertura conica
    private Vector3 posHueco;

    private float dH;
    private Vector3 nP;
    private Vector3 vP;
    private Vector3[] Vertices;
    void Start()
    { 
        //Inicilización de variables
        mTime = 0.0f;
        mMass = 1.0f;
        mStep = 0.005f;
        A= new Vector3(0.0f, 0.0f, 0.0f);
        
        //variables universales
        gravedad = -9.8f;
        Gravedad = new Vector3(0.0f, -9.8f, 0.0f);
        Origen = new Vector3(0.0f, 0.0f, 0.0f);
        Epsilon = 0.9f;
        s = 0.02f;
        miu = 0.9f;
        Ro = 4f;
        Ri = 10f;
        
        //Escenario
        mViewEscenario = new GameObject[3];
        
        //Esfera colision externa
        mViewEscenario[0] = GameObject.Find("EsferaSolida");  
        mViewEscenario[0].transform.position = new Vector3(16.0f, 16.0f, 16.0f);
        mViewEscenario[0].transform.localScale = new Vector3(Ro, Ro, Ro);
        //Esfera colision interna
        mViewEscenario[1] = GameObject.Find("EsferaHueca");
        mViewEscenario[1].transform.position = new Vector3(15.0f, 15.0f, 15.0f);
        mViewEscenario[1].transform.localScale = new Vector3(Ri, Ri, Ri)*2;
        Vector3 D = new Vector3(1 / (1 / Mathf.Sqrt(3)), (1 / Mathf.Sqrt(3)), (1 / Mathf.Sqrt(3)));
        posHueco = mViewEscenario[1].transform.position - D * 10;
        Ang = 30;
        //plano
        nP = new Vector3(1 / Mathf.Sqrt(2), 1 / Mathf.Sqrt(2), 0);
        mViewEscenario[2] = GameObject.Find("Plano");
        mViewEscenario[2].transform.position = new Vector3(0.0f, 0.0f, 0.0f);
        dH = -(Vector3.Dot(nP, mViewEscenario[2].transform.position));
        //mViewEscenario[2].transform.LookAt(nP);
        Vertices = new Vector3[3];

        Vertices[0] = new Vector3(5.0f, -10.0f, 10.0f);
        Vertices[1] = new Vector3(15.0f, 0.0f, 0.0f);
        Vertices[2] = new Vector3(10.0f, 0.0f, 0.0f);
        

        //Resorte
        Resorte.longitud = 6;
        Resorte.Ke = 0.8f;
        Resorte.Kb = 0.8f;
        
        //Particulas
        R = 1.0f;
        cantEsferas = 4;
        Particula = new Particle[cantEsferas];
        mViewEsphere = new GameObject[cantEsferas];
        for (int i=0;i<cantEsferas;i++)
        {
            Particula[i].hueco = new bool[2];
        }


        ///Unidas al resorte
        //particula 1
        Particula[0].Fase.pos = new Vector3(10.0f, 10.0f, 10.0f);
        Particula[0].Fase.vel = new Vector3(-8.0f, -5.0f, 0.0f);

        //particula 2
        Particula[1].Fase.pos = new Vector3(15.0f, 15.0f, 15.0f);
        Particula[1].Fase.vel = new Vector3(0.0f, 0.0f, 0.0f);


        ///Sueltas
        //particula 3
        Particula[2].Fase.pos = new Vector3(20.0f, 20.0f, 20.0f);
        Particula[2].Fase.vel = new Vector3(-8.0f, -5.0f, 0.0f);

        //Particula 4
        Particula[3].Fase.pos = new Vector3(22.0f, 22.0f, 22.0f);
        Particula[3].Fase.vel = new Vector3(-15.0f, -5.0f, 0.0f);




        // Busqueda de objetos visuales        
        for (int i = 0; i < cantEsferas; i++)
        {
            mViewEsphere[i] = GameObject.Find("Sphere"+i);
            mViewEsphere[i].transform.localScale = new Vector3(R, R, R);
            Particula[i].Radio = R;
            Particula[i].masa = mMass;
            Particula[i].hueco[0] = false;
            Particula[i].hueco[1] = false;

        }

        
    }
    private vectores SumaDelta(Particle particula, float dt, vectores b)
    {
        vectores Rk;
        Rk.pos = dt * particula.DFase.pos + b.pos;
        Rk.vel = dt * particula.DFase.vel + b.vel;
        return Rk;

    }
    // Start is called before the first frame update
    private void SolveMovementEc(Particle[] particulas)
    {
        /*metodo RUNGE KUTTA 4 orden
         *Yi+1=Yi+1/6(k1+2k2+2k3+k4)
         *donde:
         *k1=hf(xi,yi)
         *k2=hf(xi+1/2h,yi+1/2k2)
         *k3 = hf(xi+1/2h,yi+1/2k2)
         *k4 = hf(xi+h,yi+k3)
         */
        for (int i = 0; i < cantEsferas; i++)
        {
            k1 =particulas[i].DFase;
            k2.pos = k1.pos / 2.0f;
            k2.vel = k1.vel / 2.0f;
            k2 = SumaDelta(particulas[i],mStep / 2.0f, k2);
            k3.pos = k2.pos / 2.0f;
            k3.vel = k2.vel / 2.0f;
            k3 = SumaDelta(particulas[i],mStep / 2.0f, k3);
            k4 = SumaDelta(particulas[i],mStep, k3);

            //RUNGE KUTTA
            particulas[i].Fase.pos += (1.0f / 6.0f) * (k1.pos + 2 * k2.pos + 2 * k3.pos + k4.pos) * mStep;
            particulas[i].Fase.vel += (1.0f / 6.0f) * (k1.vel + 2 * k2.vel + 2 * k3.vel + k4.vel) * mStep;
        }
    }
    private float CalculateH(Vector3 vector, Vector3 normal, float dh)
    {
        return Vector3.Dot(vector, normal) + dh;
    }
    private bool DentroFuera(Vector3[] points)
    {
        float aux = 0;

        aux = Mathf.Acos(Vector3.Dot(points[0].normalized, points[1].normalized)) * (180f / Mathf.PI);
        aux += Mathf.Acos(Vector3.Dot(points[1].normalized, points[2].normalized)) * (180f / Mathf.PI);
        aux += Mathf.Acos(Vector3.Dot(points[2].normalized, points[0].normalized)) * (180f / Mathf.PI);

        if (Math.Abs(aux - 360) < 10)
        {
            return true;
        }
        else
        {
            return false;
        }

    }
    // Start is called before the first frame update
    private void CalculateInstantVariables()
    {
        // Solución analítica

        //Fuerzas de campo
        for(int i=0;i<cantEsferas;i++)
        {
            Particula[i].fuerza = Particula[i].Fase.pos.normalized;
            Particula[i].fuerza += Gravedad * mMass;
        }

        /*
         * SOLUCION RESORTE
         */
        Resorte.dX = Particula[0].Fase.pos - Particula[1].Fase.pos;
        Vector3 VelRel = Particula[0].Fase.vel - Particula[1].Fase.vel;
        Resorte.distancia = Resorte.dX / Resorte.dX.magnitude;
        Resorte.dL = Resorte.longitud - Resorte.dX.magnitude;
        Vector3 Fkp = Resorte.distancia * Resorte.Ke * Resorte.dL;
        Vector3 Fbp = -Resorte.distancia * Vector3.Dot(Resorte.distancia, VelRel) * Resorte.Kb;

        if (Resorte.dL < 0)
        {
            Particula[0].fuerza +=Fkp+Fbp ;
            Particula[1].fuerza += -Fkp - Fbp;
        }
        else
        {
            Particula[0].fuerza -= Fkp + Fbp;
            Particula[1].fuerza -= -Fkp - Fbp;
        }
        for (int i = 0; i < cantEsferas; i++)
        {
            //Cambio de fase
            Particula[i].DFase.pos = Particula[i].Fase.vel;        //velocidad
            Particula[i].DFase.vel = Particula[i].fuerza / Particula[i].masa;    //aceleracion
        }
        
        /*
        * SOLUCION COLISION INTERNA
        */
        Vector3[] dX = new Vector3[cantEsferas];
        float[] isDentro = new float[cantEsferas];
        Vector3[] p1 = new Vector3[cantEsferas];
        Vector3[] p2 = new Vector3[cantEsferas];
        float Jmag = mMass * (1 + Epsilon);

        for (int i = 0; i < cantEsferas; i++)
        {
            Particula[i].Impulso = Particula[i].Fase.vel * mMass;              //impulso inicial
            Particula[i].DistEnEsf1 = mViewEscenario[1].transform.position - Particula[i].Fase.pos;
            isDentro[i] = Vector3.Dot(posHueco.normalized, Particula[i].DistEnEsf1.normalized);
            isDentro[i] = Mathf.Acos(isDentro[i]) * (180f / Mathf.PI);
            p1[i] = Vector3.Normalize((Particula[i].Fase.vel - (Vector3.Dot(Particula[i].Fase.vel, Particula[i].DistEnEsf1.normalized) * Particula[i].DistEnEsf1.normalized))); //Velocidad entrante proyectada
            if(isDentro[i] < Ang / 2)
            {
               Particula[i].hueco[0] = true;
            }
           
            if ((Particula[i].DistEnEsf1.magnitude + R) > Ri && isDentro[i] > Ang / 2 && Particula[i].hueco[0] == false)
            { 
                Particula[i].Impulso += (-Jmag * (Vector3.Dot(Particula[i].DistEnEsf1.normalized, Particula[i].Fase.vel)) * Particula[i].DistEnEsf1.normalized) - (Jmag * miu * p1[i]);    //correccion del vector velocidad (impulso)
                dX[i] += ((Particula[i].DistEnEsf1.magnitude + Particula[i].Radio) - Ri) * Particula[i].DistEnEsf1.normalized;
            }
            //correcion
            Particula[i].Fase.pos += dX[i];
            Particula[i].Fase.vel = Particula[i].Impulso / mMass;

            //Cambio de fase
            Particula[i].DFase.pos = Particula[i].Fase.vel;        //velocidad
            Particula[i].DFase.vel = Particula[i].fuerza / Particula[i].masa;    //aceleracion

        }

        /*
        * SOLUCION COLISION EXERNA
        */
        //con esfera solida
        for (int i = 0; i < cantEsferas; i++)
        {
            Particula[i].Impulso = Particula[i].Fase.vel * mMass;              //impulso inicial
            Particula[i].DistEnEsf2 = mViewEscenario[0].transform.position - Particula[i].Fase.pos;
            isDentro[i] = Vector3.Dot(posHueco.normalized, Particula[i].DistEnEsf2.normalized);
            isDentro[i] = Mathf.Acos(isDentro[i]) * (180f / Mathf.PI);
            p2[i] = Vector3.Normalize((Particula[i].Fase.vel - (Vector3.Dot(Particula[i].Fase.vel, Particula[i].DistEnEsf2.normalized) * Particula[i].DistEnEsf2.normalized))); //Velocidad entrante proyectada
            if ((Particula[i].DistEnEsf2.magnitude + R) > Ro && isDentro[i] > Ang / 2)
            {
                Particula[i].Impulso += (-Jmag * (Vector3.Dot(Particula[i].DistEnEsf2.normalized, Particula[i].Fase.vel)) * Particula[i].DistEnEsf2.normalized) - (Jmag * miu * p2[i]);    //correccion del vector velocidad (impulso)
                dX[i] += ((Particula[i].DistEnEsf2.magnitude + Particula[i].Radio) - Ri) * Particula[i].DistEnEsf2.normalized;
            }
            Particula[i].Fase.pos += dX[i];
            Particula[i].Fase.vel = Particula[i].Impulso / mMass;

            //Cambio de fase
            Particula[i].DFase.pos = Particula[i].Fase.vel;        //velocidad
            Particula[i].DFase.vel = Particula[i].fuerza / Particula[i].masa;    //aceleracion
        }
        //entre particulas
        
        for (int i = 0; i < cantEsferas; i++)
        {
            for (int j = 0; j < cantEsferas; j++)
            {
                Particula[i].Impulso = Particula[i].Fase.vel * mMass;              //impulso inicial
                Particula[i].DistEnEsf2 = Particula[j].Fase.pos - Particula[i].Fase.pos;
                p2[i] = Vector3.Normalize((Particula[i].Fase.vel - (Vector3.Dot(Particula[i].Fase.vel, Particula[i].DistEnEsf2.normalized) * Particula[i].DistEnEsf2.normalized))); //Velocidad entrante proyectada
                if ((Particula[i].DistEnEsf2.magnitude + R) < R)
                {
                    Particula[i].Impulso += (-Jmag * (Vector3.Dot(Particula[i].DistEnEsf2.normalized, Particula[i].Fase.vel)) * Particula[i].DistEnEsf2.normalized) - (Jmag * miu * p2[i]);    //correccion del vector velocidad (impulso)
                    dX[i] += ((Particula[i].DistEnEsf2.magnitude + Particula[i].Radio) - R) * Particula[i].DistEnEsf2.normalized;
                }
                Particula[i].Fase.pos += dX[i];
                Particula[i].Fase.vel = Particula[i].Impulso / mMass;

                //Cambio de fase de las particulas
                Particula[i].DFase.pos = Particula[i].Fase.vel;        //velocidad
                Particula[i].DFase.vel = Particula[i].fuerza / Particula[i].masa;    //aceleracion
            }

        }

        /*
        * SOLUCION COLISION PLANO
        */
        Vector3[] vP = new Vector3[cantEsferas];
        Vector3[] dX1 = new Vector3[cantEsferas];
        Vector3[] dentrofuera = new Vector3[cantEsferas];
        float[] H=new float[cantEsferas];
        bool[] isdentro=new bool[cantEsferas];
        for (int i = 0; i < cantEsferas; i++)
        {
            vP[i] = Vector3.Normalize(Particula[i].Fase.vel - (Vector3.Dot(Particula[i].Fase.vel, nP) * nP));
            Particula[i].Impulso = Particula[i].Fase.vel * mMass;
            H[i] = CalculateH(Particula[i].Fase.vel, nP, dH);
            for (int k = 0; k < cantEsferas; k++)
            {
                for (int j = 0; j < 3; j++)
                {
                    dentrofuera[k] = Vector3.Normalize(Particula[k].Fase.pos - Vertices[j]);
                }
            }
            for(int j=0;j<cantEsferas;j++)
            {
                isdentro[j] = DentroFuera(dentrofuera);
                if (isdentro[j] == true)
                {
                    Particula[j].hueco[1] = true;
                }
            }
            
            if (H[i] - Particula[i].Radio<0&& isdentro[i]==false &&Particula[i].hueco[1]==false)
            {
                Particula[i].Impulso += (-Jmag * (Vector3.Dot(nP, Particula[i].Fase.vel)) * nP) - (Jmag * miu * vP[i]);
                dX1[i] += (R - H[i]) * nP;
            }
            
            //correcion
            Particula[i].Fase.pos += dX1[i];
            Particula[i].Fase.vel = Particula[i].Impulso / mMass;
            
            //Cambio de fase
            Particula[i].DFase.pos = Particula[i].Fase.vel;        //velocidad
            Particula[i].DFase.vel = Particula[i].fuerza / Particula[i].masa;    //aceleracion
            

        }








    }

    // Update is called once per frame
    void Update()
    { 
        Debug.Log("Tiempo  "  + mTime);   // Imprimir el tiempo
       
        CalculateInstantVariables();
        SolveMovementEc(Particula);

        for (int i = 0; i < cantEsferas; i++)
        {
            mViewEsphere[i].transform.position = Particula[i].Fase.pos;
        }
        mTime += mStep;
    }
}//
//