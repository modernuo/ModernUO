using System;
using System.Collections;
using System.Data.Odbc;
using System.Threading;

namespace Server.Engines.MyRunUO
{
  public class DatabaseCommandQueue
  {
    private string m_CompletionString;
    private string m_ConnectionString;
    private Queue m_Queue;
    private ManualResetEvent m_Sync;

    public DatabaseCommandQueue(string completionString, string threadName) : this(Config.CompileConnectionString(),
      completionString, threadName)
    {
    }

    public DatabaseCommandQueue(string connectionString, string completionString, string threadName)
    {
      m_CompletionString = completionString;
      m_ConnectionString = connectionString;

      m_Queue = Queue.Synchronized(new Queue());

      m_Queue.Enqueue(null); // signal connect

      /*m_Queue.Enqueue( "DELETE FROM myrunuo_characters" );
      m_Queue.Enqueue( "DELETE FROM myrunuo_characters_layers" );
      m_Queue.Enqueue( "DELETE FROM myrunuo_characters_skills" );
      m_Queue.Enqueue( "DELETE FROM myrunuo_guilds" );
      m_Queue.Enqueue( "DELETE FROM myrunuo_guilds_wars" );*/

      m_Sync = new ManualResetEvent(true);

      // MyRunUO Database Command Queue;
      Thread thread = new Thread(Thread_Start) { Name = threadName, Priority = Config.DatabaseThreadPriority };
      thread.Start();
    }

    public bool HasCompleted{ get; private set; }

    public void Enqueue(object obj)
    {
      lock (m_Queue.SyncRoot)
      {
        m_Queue.Enqueue(obj);
        try
        {
          m_Sync.Set();
        }
        catch
        {
          // ignored
        }
      }
    }

    private void Thread_Start()
    {
      bool connected = false;

      OdbcConnection connection = null;
      OdbcCommand command = null;
      OdbcTransaction transact = null;

      DateTime start = DateTime.UtcNow;

      bool shouldWriteException = true;

      while (true)
      {
        m_Sync.WaitOne();

        while (m_Queue.Count > 0)
          try
          {
            object obj = m_Queue.Dequeue();

            if (obj == null)
            {
              if (connected)
              {
                if (transact != null)
                  try
                  {
                    transact.Commit();
                  }
                  catch (Exception commitException)
                  {
                    Console.WriteLine("MyRunUO: Exception caught when committing transaction");
                    Console.WriteLine(commitException);

                    try
                    {
                      transact.Rollback();
                      Console.WriteLine("MyRunUO: Transaction has been rolled back");
                    }
                    catch (Exception rollbackException)
                    {
                      Console.WriteLine("MyRunUO: Exception caught when rolling back transaction");
                      Console.WriteLine(rollbackException);
                    }
                  }

                try
                {
                  connection.Close();
                }
                catch
                {
                  // ignored
                }

                try
                {
                  connection.Dispose();
                }
                catch
                {
                  // ignored
                }

                try
                {
                  command.Dispose();
                }
                catch
                {
                  // ignored
                }

                try
                {
                  m_Sync.Close();
                }
                catch
                {
                  // ignored
                }

                Console.WriteLine(m_CompletionString, (DateTime.UtcNow - start).TotalSeconds);
                HasCompleted = true;

                return;
              }

              try
              {
                connected = true;
                connection = new OdbcConnection(m_ConnectionString);
                connection.Open();
                command = connection.CreateCommand();

                if (Config.UseTransactions)
                {
                  transact = connection.BeginTransaction();
                  command.Transaction = transact;
                }
              }
              catch (Exception e)
              {
                try
                {
                  transact?.Rollback();
                }
                catch
                {
                  // ignored
                }

                try
                {
                  connection?.Close();
                }
                catch
                {
                  // ignored
                }

                try
                {
                  connection?.Dispose();
                }
                catch
                {
                  // ignored
                }

                try
                {
                  command?.Dispose();
                }
                catch
                {
                  // ignored
                }

                try
                {
                  m_Sync.Close();
                }
                catch
                {
                  // ignored
                }

                Console.WriteLine("MyRunUO: Unable to connect to the database");
                Console.WriteLine(e);
                HasCompleted = true;
                return;
              }
            }
            else if (obj is string s)
            {
              command.CommandText = s;
              command.ExecuteNonQuery();
            }
            else
            {
              string[] parms = (string[])obj;

              command.CommandText = parms[0];

              if (command.ExecuteScalar() == null)
              {
                command.CommandText = parms[1];
                command.ExecuteNonQuery();
              }
            }
          }
          catch (Exception e)
          {
            if (shouldWriteException)
            {
              Console.WriteLine("MyRunUO: Exception caught in database thread");
              Console.WriteLine(e);
              shouldWriteException = false;
            }
          }

        lock (m_Queue.SyncRoot)
        {
          if (m_Queue.Count == 0)
            m_Sync.Reset();
        }
      }
    }
  }
}