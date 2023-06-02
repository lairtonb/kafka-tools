using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaTools.Logging
{
    public class CircularBuffer<T>
    {
        private readonly T[] buffer;
        private int head;
        private int tail;
        private int count;

        public CircularBuffer(int capacity)
        {
            buffer = new T[capacity];
        }

        public int Count => count;

        public void Enqueue(T item)
        {
            buffer[tail] = item;
            tail = (tail + 1) % buffer.Length;

            if (count == buffer.Length)
            {
                head = (head + 1) % buffer.Length;
            }
            else
            {
                count++;
            }
        }

        public IEnumerable<T> GetItems()
        {
            if (count == 0)
                yield break;

            if (tail > head)
            {
                for (int i = head; i < tail; i++)
                {
                    yield return buffer[i];
                }
            }
            else
            {
                for (int i = head; i < buffer.Length; i++)
                {
                    yield return buffer[i];
                }

                for (int i = 0; i < tail; i++)
                {
                    yield return buffer[i];
                }
            }
        }
    }

}
