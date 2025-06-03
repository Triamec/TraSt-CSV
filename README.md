# TraSt-CSV
This C# console application demonstrates trajectory streaming (TraSt) for motion control systems. It enables users to stream predefined motion paths - loaded from a CSV file- to one or more motiion axes. The application continuously sends trajectory segments to the drives and provides feedback. At any time, the user can safely abort the streaming process by pressin 'q'. 

This example project illustrates how to use the TraSt feature of the TAM API and serves as a beginner-friendly introduction to basic trajectory streaming workflows. 

## What is Trajectory Streaming
Trajectory streaming is a technique used to execute complex motion profiles in real time by separating path calculation from execution. 
In this application, the motion path is precomputed and stored in a CSV file. 

The host system reads this file and divides the trajectory into small segments. These segments are continuously sent to a ring buffer on the drive. While the host keeps loading new segments into the buffer, the drive reads and executes them in real time - ensuriog smooth, uninterrupted motion. This enables high-frequency motion, even though the host system itself is not real-time capable. The TAM API manages the streaming process and ensures the buffer never overflows or underflows, allowing seamless real-time execution on the drive side.

The following image illustrates this workflow and shows the interaction between the host PC and the drive:

![TraSt_Overview](./doc/TraSt_Overview.png)
