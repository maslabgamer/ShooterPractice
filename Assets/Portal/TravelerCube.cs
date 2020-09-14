using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TravelerCube : PortalTraveler {
    // Update is called once per frame
    void Update() {
        transform.position += transform.forward * Time.deltaTime * 2f;
    }
}
