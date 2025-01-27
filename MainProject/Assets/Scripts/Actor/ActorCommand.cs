//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18052
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using UnityEngine;

//Encapsulates a single command to an actor. The actor will choose how to respond to the message.
public class ActorCommand
{
	//All possible commands an actor can be given.
	public enum CommandType
	{
		MOVE,		//Says to move the actor by the given vector.
		ROTATE,		//Says to rotate the actor by the given vector; each vector component represents a rotation around the component's respective axis.
		//MOVETO,	//Says to move the actor to the given position.
		SHIV,		//Says to stab directly in front of the actor.
		HANDSUP		//Says to put your hands up!
	};

	private CommandType type;
	private Vector3 vector;

	public CommandType Type { get { return type; } }
	public Vector3 Vector { get { return vector; } }

	public ActorCommand (CommandType pType, Vector3 pVec)
	{
		type = pType;
		vector = pVec;
	}
}