using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace MetaWear.Android
{
    class BluetoothLeGattQueue
    {
        private class GattOp
        {
            internal String msg;
            internal BluetoothLeGatt owner;
            internal Func<Task> task;
            internal TaskCompletionSource<byte[]> taskSource;

            internal GattOp(string msg, BluetoothLeGatt owner, Func<Task> task)
            {
                this.msg = msg;
                this.owner = owner;
                this.task = task;
                taskSource = new TaskCompletionSource<byte[]>();
            }
        }

        // TODO: I don't support mupltiple devices, but probably should
        private readonly ConcurrentQueue<GattOp> pendingGattOps = new ConcurrentQueue<GattOp>();

        internal Task<byte[]> AddGattOperation(BluetoothLeGatt owner, String msg, Func<Task> task)
        {
            Interlocked.Increment(ref owner.nGattOps);

            GattOp newGattOp = new GattOp(msg, owner, task);
            pendingGattOps.Enqueue(newGattOp);
            ExecuteGattOperation(false);

            return newGattOp.taskSource.Task;
        }

        private void ExecuteGattOperation(bool ready)
        {
            if (!pendingGattOps.IsEmpty && (pendingGattOps.Count == 1 || ready))
            {
                if (pendingGattOps.TryDequeue(out var next))
                {
                    // I'm sure we are duplicating basic task functionality here.
                    next.owner.gattOpTask
                        // ERROR: here we use 1000ms, and ignore the TimeForResponse figure
                        .Execute(next.msg, 3000, next.task)
                        .ContinueWith(task => {
                            // By here, the task is complete.
                            if (task.IsFaulted)
                            {
                                next.taskSource.SetException(task.Exception);
                            }
                            else if (task.IsCanceled)
                            {
                                next.taskSource.SetCanceled();
                            }
                            else
                            {
                                var result = task.GetAwaiter().GetResult();
                                next.taskSource.SetResult(result);
                            }

                        
                            next.owner.GattTaskCompleted();

                            ExecuteGattOperation(true);
                    });
                }

            }
        }
    }
}