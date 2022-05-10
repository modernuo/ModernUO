---
title: Timers
---

# Timers

ModernUO completely changed the timer system to use an optimized data structure called a timer wheel. This will allow shards to add thousands of timers without slowing down the server. Traditionally the timer system used a thread and locked to add/remove/process timers. All of this is gone.
With the new timer system there is no TimerPriority. This can be deleted entirely from your scripts.
