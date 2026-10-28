using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cliente: entra por la puerta, recorre pasillos (siempre por el pasillo transversal y luego por el pasillo elegido),
/// toma productos, se forma en la fila mas corta, paga y sale. Sin NavMesh: usa waypoints ortogonales.
/// </summary>
public class Customer : MonoBehaviour
{
    public enum State { Entering, Shopping, Grabbing, WaitingForLane, GoingToQueue, Queueing, Leaving }

    public State state;
    public int itemsBought;
    public bool AtCounter;

    PandemicDirector dir;
    PersonRig rig;
    ShoppingCart cart;
    CheckoutLane lane;
    ShelfUnit targetShelf;

    bool panic;
    float speed;
    int itemsWanted;
    float crossOffset;           // cada cliente usa un carril distinto del pasillo transversal
    float grabTimer;
    int grabsLeft;
    float retryTimer;
    readonly List<Vector3> path = new List<Vector3>();
    int pathIndex;

    // ------------------------------------------------------------------ creacion
    public static Customer Create(PandemicDirector d, PandemicPhase ph)
    {
        Vector3 spawn = new Vector3(Random.Range(-11f, 11f), 0f, -34f);
        PersonRig rig = PersonFactory.Create("Cliente", d.customersRoot, spawn);
        Customer c = rig.gameObject.AddComponent<Customer>();
        c.dir = d;
        c.rig = rig;
        c.panic = ph.panic;
        c.speed = Random.Range(ph.speedMin, ph.speedMax);
        c.itemsWanted = Random.Range(ph.itemsMin, ph.itemsMax + 1);
        c.crossOffset = Random.Range(-0.7f, 0.7f);
        c.cart = ShoppingCart.Create(rig.transform);
        c.cart.maxScale = d.cartMaxScale;
        c.cart.itemsForMaxScale = d.itemsForMaxScale;
        rig.pushing = true;

        // camino de la calle hasta la puerta
        float dx = Random.Range(-1.2f, 1.2f);
        c.path.Add(new Vector3(spawn.x * 0.4f, 0f, -26f));
        c.path.Add(new Vector3(dx, 0f, -21.5f));
        c.path.Add(new Vector3(dx, 0f, -17.6f));
        c.state = State.Entering;
        return c;
    }

    // ------------------------------------------------------------------ bucle principal
    void Update()
    {
        bool moving = false;
        switch (state)
        {
            case State.Entering:
                moving = true;
                if (FollowPath()) PlanNextShelf();
                break;

            case State.Shopping:
                moving = true;
                if (FollowPath()) BeginGrab();
                break;

            case State.Grabbing:
                grabTimer -= Time.deltaTime;
                if (grabTimer <= 0f) DoGrab();
                break;

            case State.WaitingForLane:
                moving = pathIndex < path.Count;
                FollowPath();
                retryTimer -= Time.deltaTime;
                if (retryTimer <= 0f) GoToQueue();
                break;

            case State.GoingToQueue:
                moving = true;
                if (FollowPath()) state = State.Queueing;
                break;

            case State.Queueing:
                moving = QueueMove();
                break;

            case State.Leaving:
                moving = true;
                if (FollowPath())
                {
                    dir.OnCustomerLeft(this);
                    Destroy(gameObject);
                }
                break;
        }
        rig.walkSpeed = moving ? speed : 0f;
    }

    // ------------------------------------------------------------------ compras
    void PlanNextShelf()
    {
        targetShelf = dir.PickShelf(panic);
        if (targetShelf == null) { FinishShopping(); return; }

        Vector3 p = transform.position;
        float standX = targetShelf.aisleX - targetShelf.side * 0.75f + Random.Range(-0.2f, 0.2f);
        float standZ = targetShelf.zCenter + Random.Range(-0.6f, 0.6f);
        float crossZ = StoreLayout.CrossZ + crossOffset;

        path.Clear();
        pathIndex = 0;
        if (p.z > StoreLayout.CrossZ + 0.9f || Mathf.Abs(p.z - crossZ) > 0.3f) path.Add(new Vector3(p.x, 0f, crossZ));
        path.Add(new Vector3(standX, 0f, crossZ));
        path.Add(new Vector3(standX, 0f, standZ));
        state = State.Shopping;
    }

    void BeginGrab()
    {
        state = State.Grabbing;
        grabsLeft = panic ? Random.Range(2, 6) : 1;
        grabTimer = panic ? Random.Range(0.2f, 0.4f) : Random.Range(0.7f, 1.3f);
        // que brazo extender: el que queda del lado de la repisa
        Vector3 shelfDirLocal = transform.InverseTransformDirection(new Vector3(-targetShelf.side, 0f, 0f));
        rig.reach = shelfDirLocal.x > 0f ? 1f : -1f;
    }

    void DoGrab()
    {
        string id = targetShelf != null ? targetShelf.TryTake() : null;
        if (id != null)
        {
            cart.AddItem(id);
            itemsBought++;
            dir.OnItemTaken();
            grabsLeft--;
            grabTimer = panic ? Random.Range(0.12f, 0.25f) : Random.Range(0.5f, 0.9f);
        }
        else grabsLeft = 0;

        if (grabsLeft <= 0 || itemsBought >= itemsWanted)
        {
            rig.reach = 0f;
            targetShelf = null;
            if (itemsBought >= itemsWanted) FinishShopping(); else PlanNextShelf();
        }
    }

    void FinishShopping()
    {
        rig.reach = 0f;
        if (itemsBought == 0) { dir.OnLeftEmptyHanded(); LeaveStore(); }
        else GoToQueue();
    }

    // ------------------------------------------------------------------ fila y caja
    void GoToQueue()
    {
        lane = dir.ChooseLane();
        Vector3 p = transform.position;
        float crossZ = StoreLayout.CrossZ + crossOffset;
        path.Clear();
        pathIndex = 0;

        if (lane == null)
        {
            // todas las filas llenas: esperar en el pasillo transversal y reintentar
            if (p.z > StoreLayout.CrossZ + 0.9f) path.Add(new Vector3(p.x, 0f, crossZ));
            state = State.WaitingForLane;
            retryTimer = 1f;
            return;
        }
        lane.Join(this);
        if (p.z > StoreLayout.CrossZ + 0.9f || Mathf.Abs(p.z - crossZ) > 0.3f) path.Add(new Vector3(p.x, 0f, crossZ));
        path.Add(new Vector3(lane.laneX, 0f, crossZ));
        state = State.GoingToQueue;
    }

    bool QueueMove()
    {
        int idx = lane.queue.IndexOf(this);
        if (idx < 0) return false;
        Vector3 slot = lane.SlotPosition(idx);
        Vector3 pos = transform.position;
        Vector3 to = slot - pos; to.y = 0f;
        float d = to.magnitude;
        if (d > 0.04f)
        {
            float step = Mathf.Min(d, speed * Time.deltaTime);
            transform.position = pos + to / d * step;
            Face(to / d);
            return true;
        }
        Face(Vector3.back);           // mirar hacia la cajera
        AtCounter = idx == 0;
        return false;
    }

    public float ServiceTime() { return 0.6f + itemsBought * 0.08f; }

    public void FinishPayment()
    {
        AtCounter = false;
        float x = lane.laneX;
        float sign = x < -10f ? 1f : (x > 10f ? -1f : (x < 0f ? -1f : 1f));
        float ex = x + sign * 1.7f;
        float rx = Random.Range(-1f, 1f);
        path.Clear();
        pathIndex = 0;
        path.Add(new Vector3(ex, 0f, StoreLayout.QueueStartZ));
        path.Add(new Vector3(ex, 0f, -17.4f));
        path.Add(new Vector3(rx, 0f, -17.4f));
        path.Add(new Vector3(rx, 0f, -22f));
        path.Add(new Vector3(rx * 6f, 0f, -34f));
        state = State.Leaving;
    }

    void LeaveStore()
    {
        Vector3 p = transform.position;
        float rx = Random.Range(-1f, 1f);
        path.Clear();
        pathIndex = 0;
        if (p.z > StoreLayout.CrossZ + 0.9f) path.Add(new Vector3(p.x, 0f, StoreLayout.CrossZ + crossOffset));
        path.Add(new Vector3(rx, 0f, StoreLayout.CrossZ + crossOffset));
        path.Add(new Vector3(rx, 0f, -22f));
        path.Add(new Vector3(rx * 6f, 0f, -34f));
        state = State.Leaving;
    }

    // ------------------------------------------------------------------ movimiento
    bool FollowPath()
    {
        if (pathIndex >= path.Count) return true;
        if (StepTo(path[pathIndex])) pathIndex++;
        return pathIndex >= path.Count;
    }

    bool StepTo(Vector3 target)
    {
        Vector3 pos = transform.position;
        Vector3 to = target - pos; to.y = 0f;
        float d = to.magnitude;
        float step = speed * Time.deltaTime;
        if (d <= step + 0.001f)
        {
            transform.position = new Vector3(target.x, 0f, target.z);
            return true;
        }
        Vector3 dirN = to / d;
        transform.position = pos + dirN * step;
        Face(dirN);
        return false;
    }

    void Face(Vector3 d)
    {
        if (d.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Mathf.Clamp01(Time.deltaTime * 10f));
    }
}
