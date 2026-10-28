using System.Collections.Generic;
using UnityEngine;

/// <summary>Una caja registradora con su fila. Atiende al primero cuando este llega al mostrador.</summary>
public class CheckoutLane : MonoBehaviour
{
    public float laneX;
    public PandemicDirector director;
    public List<Customer> queue = new List<Customer>();

    Customer serving;
    float serviceTimer;

    public bool IsFull { get { return queue.Count >= StoreLayout.QueueMax; } }

    public Vector3 SlotPosition(int i)
    {
        return new Vector3(laneX, 0f, StoreLayout.QueueStartZ + i * StoreLayout.QueueSpacing);
    }

    public void Join(Customer c) { queue.Add(c); }

    void Update()
    {
        if (queue.Count == 0) { serving = null; return; }
        Customer head = queue[0];
        if (!head.AtCounter) return;

        if (serving != head)
        {
            serving = head;
            serviceTimer = head.ServiceTime();
        }
        serviceTimer -= Time.deltaTime;
        if (serviceTimer <= 0f)
        {
            queue.RemoveAt(0);
            serving = null;
            if (director != null) director.OnPaid(head.itemsBought);
            head.FinishPayment();
        }
    }
}
