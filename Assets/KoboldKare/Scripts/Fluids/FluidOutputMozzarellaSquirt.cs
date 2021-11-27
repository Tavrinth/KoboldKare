using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(Mozzarella))]
public class FluidOutputMozzarellaSquirt : FluidOutput {
    public enum OutputType {
        Squirt,
        Hose,
        Splash,
    }
    [SerializeField]
    private OutputType type;
    private Mozzarella mozzarella;
    private MozzarellaRenderer mozzarellaRenderer;
    private FluidHitListener fluidHitListener;
    [Range(0.1f,10f)]
    [SerializeField]
    private float squirtDuration = 0.5f;
    private int currentIndex;
    [SerializeField]
    private AnimationCurve volumeCurve;
    [SerializeField]
    private AnimationCurve velocityCurve;
    [SerializeField][Range(0f,1f)]
    private float velocityMultiplier = 0.1f;
    [SerializeField][Range(0f,1f)]
    private float velocityVariance = 0f;
    private WaitForSeconds waitForSeconds = new WaitForSeconds(1f);
    private List<Coroutine> fireRoutines;
    [SerializeField]
    private VisualEffect effect;
    [SerializeField]
    private float vps = 2f;
    public void Fire() {
        var container = GetComponentInParent<GenericReagentContainer>();
        if (container != null) {
            Fire(container.contents);
        }
    }
    public override void Fire(ReagentContents b) {
        for(int i=0;i<fireRoutines.Count;i++) {
            if (fireRoutines[i] == null) {
                fireRoutines.RemoveAt(i);
            }
        }
        if (b.volume <= 0f){ 
            return;
        }
        Color c = b.GetColor(ReagentDatabase.instance);
        effect.SetVector4("Color", c);
        mozzarellaRenderer.material.color = c;
        fluidHitListener.projector.color = c;
        switch(type) {
            case OutputType.Squirt: fireRoutines.Add(StartCoroutine(FireRoutine(b))); break;
            case OutputType.Hose: fireRoutines.Add(StartCoroutine(Hose(b))); break;
            case OutputType.Splash: fireRoutines.Add(StartCoroutine(Splash(b, vps*squirtDuration, squirtDuration))); break;
        }
    }
    public override void StopFiring() {
        foreach(Coroutine routine in fireRoutines) {
            if (routine != null) {
                StopCoroutine(routine);
            }
        }
        effect.Stop();
        fireRoutines.Clear();
    }
    void Start() {
        effect.Stop();
        currentIndex = 0;
        mozzarella = GetComponent<Mozzarella>();
        mozzarellaRenderer = GetComponent<MozzarellaRenderer>();
        fluidHitListener = GetComponent<FluidHitListener>();
        fireRoutines = new List<Coroutine>();
    }
    IEnumerator FireRoutine(ReagentContents b) {
        while(b.volume > 0f) {
            SetRadius(Mathf.Clamp(b.volume*0.05f, 0.02f, 0.2f));
            if (b.volume > 30f) {
                Squirt();
            }
            if (b.volume > 10f) {
                Squirt();
            }
            Squirt();
            b.Spill(vps);
            yield return waitForSeconds;
        }
    }
    private void SetRadius( float radius ) {
        mozzarellaRenderer.SetPointRadius(radius);
        //mozzarella.SetVisco(radius);
        fluidHitListener.decalSize = radius;
    }
    IEnumerator Splash(ReagentContents b, float amount, float duration) {
        effect.Play();
        float targetVolume = Mathf.Max(b.volume-amount,0f);
        SetRadius(Mathf.Clamp(b.volume*0.02f, 0.01f, 0.2f));
        float startTime = Time.time;
        while(Time.time < startTime+duration) {
            float t = (Time.time-startTime)/duration;
            for(int i=0;i<mozzarella.squirts.Count;i++) {
                float volume = volumeCurve.Evaluate(t);
                mozzarella.squirts[i] = new Mozzarella.Squirt(transform.position,
                transform.forward*velocityCurve.Evaluate(t)*velocityMultiplier+UnityEngine.Random.insideUnitSphere*velocityVariance,
                volume,
                mozzarella.squirts[i].index);
            }
            b.Spill(vps*Time.deltaTime);
            yield return null;
        }
        for(int i=0;i<mozzarella.squirts.Count;i++) {
            mozzarella.squirts[i] = new Mozzarella.Squirt(transform.position, Vector3.zero, 0f, mozzarella.squirts[i].index);
        }
        effect.Stop();
    }
    IEnumerator Hose(ReagentContents b) {
        effect.Play();
        SetRadius(Mathf.Clamp(b.volume*0.01f, 0.01f, 0.2f));
        while(b.volume > 0f) {
            for(int i=0;i<mozzarella.squirts.Count;i++) {
                float volume = Mathf.Clamp01(Mathf.Abs(Mathf.Sin(Time.time*2f+i*5f))-0.2f);
                mozzarella.squirts[i] = new Mozzarella.Squirt(transform.position,
                transform.forward*velocityMultiplier+UnityEngine.Random.insideUnitSphere*velocityVariance,
                volume,
                mozzarella.squirts[i].index);
            }
            b.Spill(vps*Time.deltaTime);
            yield return null;
        }
        for(int i=0;i<mozzarella.squirts.Count;i++) {
            mozzarella.squirts[i] = new Mozzarella.Squirt(transform.position, Vector3.zero, 0f, mozzarella.squirts[i].index);
        }
        effect.Stop();
    }
    IEnumerator Squirt(int i, float duration) {
        //effect.Play();
        float startTime = Time.time;
        while(Time.time < startTime+duration) {
            float t = (Time.time-startTime)/duration;
            float volume = volumeCurve.Evaluate(t);
            mozzarella.squirts[i] = new Mozzarella.Squirt(transform.position,
            transform.forward*velocityCurve.Evaluate(t)*velocityMultiplier+UnityEngine.Random.insideUnitSphere*velocityVariance,
            volume,
            mozzarella.squirts[i].index);
            yield return null;
        }
        mozzarella.squirts[i] = new Mozzarella.Squirt(transform.position, Vector3.zero, 0f, mozzarella.squirts[i].index);
        //effect.Stop();
    }
    public void Squirt() {
        StartCoroutine(Squirt(currentIndex, squirtDuration));
        currentIndex = (++currentIndex)%(mozzarella.squirts.Count-1);
    }
}
