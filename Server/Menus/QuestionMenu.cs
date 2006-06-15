/***************************************************************************
 *                              QuestionMenu.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using Server.Network;

namespace Server.Menus.Questions
{
	public class QuestionMenu : IMenu
	{
		private string m_Question;
		private string[] m_Answers;

		private int m_Serial;
		private static int m_NextSerial;

		int IMenu.Serial
		{
			get
			{
				return m_Serial;
			}
		}

		int IMenu.EntryLength
		{
			get
			{
				return m_Answers.Length;
			}
		}

		public string Question
		{
			get
			{
				return m_Question;
			}
			set
			{
				m_Question = value;
			}
		}

		public string[] Answers
		{
			get
			{
				return m_Answers;
			}
		}

		public QuestionMenu( string question, string[] answers )
		{
			m_Question = question;
			m_Answers = answers;

			do
			{
				m_Serial = ++m_NextSerial;
				m_Serial &= 0x7FFFFFFF;
			} while ( m_Serial == 0 );
		}

		public virtual void OnCancel( NetState state )
		{
		}

		public virtual void OnResponse( NetState state, int index )
		{
		}

		public void SendTo( NetState state )
		{
			state.AddMenu( this );
			state.Send( new DisplayQuestionMenu( this ) );
		}
	}
}