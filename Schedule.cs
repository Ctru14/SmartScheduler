using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartScheduler
{
    public class SmartSchedule
    {
        public Dictionary<DateTime, LinkedList<SmartTask>> smartSchedule;

        public SmartSchedule()
        {
            smartSchedule = new Dictionary<DateTime, LinkedList<SmartTask>>();
        }

        public void AddTask(SmartTask task)
        {
            LinkedList<SmartTask> taskList;
            DateTime date = task.when.Date;

            // Add new task - inset into LinkedList 
            if (smartSchedule.TryGetValue(date, out taskList))
            {
                // Schedule already exists for this day -> add event
                // Find value in list that it should be inserted after

                // Check edge case: first and last
                if (task.when < taskList.First.Value.when)
                {
                    // Insert first
                    taskList.AddFirst(task);
                } 
                else if (task.when >= taskList.Last.Value.when)
                {
                    // Insert last
                    taskList.AddLast(task);
                } 
                else
                {
                    // Somewhere in the middle
                    var tasks = taskList.GetEnumerator();
                    LinkedListNode<SmartTask> prev = taskList.First;
                    LinkedListNode<SmartTask> next = taskList.First;
                    //  Loop until times of prev < task < next
                    do
                    {
                        prev = next;
                        if (next == null)
                        {
                            // You've reached the end of the list!
                            break;
                        }
                        next = next.Next;

                    } while (!(prev.Value.when < task.when && task.when < next.Value.when));
                    taskList.AddAfter(prev, task);
                }
                                
            } 
            else
            {
                // This day does not yet exist in our schedule -> create it
                taskList = new LinkedList<SmartTask>();
                taskList.AddFirst(task);
                smartSchedule.Add(date, taskList);
            }
        }

        public int GetTaskCount(DateTime date)
        {
            LinkedList<SmartTask> taskList;
            if (this.smartSchedule.TryGetValue(date, out taskList))
            {
                return taskList.Count;
            }
            else
            {
                return 0;
            }
        }

    } // End of class definition


} // End of SmartScheduler namespace
