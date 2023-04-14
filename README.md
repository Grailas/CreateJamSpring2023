# CreateJamSpring2023


# Concept
Fleet of ships that need to traverse the open sea to reach safety.

Controls are indirect, based on a collection of steering behaviours.

The weight of each behaviour is based on how much crew is assigned to perform at specific task, which each ship assumed to have 10 crew.

All crew can be assigned to steering the ship, but at a detriment to the wellbeing of the crew. To maintain readiness, some crew should be allowed to rest, cook, etc. 

When readiness drops, the overall performance of the crew drops, gradually rendering the steering behaviours ineffective.

## Steering forces
- Wind (use sails to move forward, strength based on alignment with  wind)
- Oars (brute force forward, exhausting)
- Separation (steer ships away from each other)
- Avoidance (steer ships away from hazards straight ahead)
- Cohesion (keep the fleet together)
- Alignment (maintain same heading as the rest of the fleet)
- Direct (orient the ships towards a specific direction)

In additon, there is always:
- Undercurrent
- Waves

## Other tasks
- Fish (gather raw fish)
- Cook (create rations)
- Pray (try to appease the gods of the sea and sky)
- Maintain (slowly fix damage and prevent sinking)
- Rest (improve readiness at a faster rate)
- Man cannons (reload, aim, and fire against threats)