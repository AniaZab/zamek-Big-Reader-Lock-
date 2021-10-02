using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Big_Reader_Lock
{
	class InvalidUnlockOperation : Exception
	{
		public InvalidUnlockOperation() { }
	}

	class BigReaderLock
	{
		Random rng = new Random();
		RWLock[] TableOfLocks;
		int NumberOfWriters;
		int NumberOfReaders;
		int NumberOfLocks;
		int NumberOfAvailableLockers;
		SemaphoreSlim ChangeNumberOfAvailableLockers = new SemaphoreSlim(1, 1);
		public BigReaderLock(RWLock[] tableOfLocks, int numberOfReaders, int numberOfWriters)
		{
			NumberOfLocks = tableOfLocks.Length;
			TableOfLocks = new RWLock[NumberOfLocks];
			NumberOfReaders = numberOfReaders;
			NumberOfWriters = numberOfWriters;
			NumberOfAvailableLockers = NumberOfLocks;
			for (int i = 0; i < NumberOfLocks; i++)
			{
				TableOfLocks[i] = new RWLock();

			}

		}
		// wykonuje równolegle funkcje za pomocą wątków
		public int[] DoThreadingWork(int numberOfThreads, int numberOfReaders, int numberOfWriters)
		{
			Thread[] threads = new Thread[numberOfThreads];
			// inicjuje wątki
			for (int j = 0; j < numberOfThreads; j++)
			{
				if (numberOfReaders > 0)
				{
					// z racji tego, że znacznie więcej jest czytaczy niż pisaczy,
					// chciałam by było większe prawdopodobieństwo wylosowania readera
					if (rng.Next(0, 5) > 0.3)
					{
						numberOfReaders--;
						threads[j] = new Thread(this.Reader);
					}
					else if (numberOfWriters > 0)
					{
						numberOfWriters--;
						threads[j] = new Thread(this.Writer);
					}
					else
					{
						numberOfReaders--;
						threads[j] = new Thread(this.Reader);
					}
				}
				else
				{
					numberOfWriters--;
					threads[j] = new Thread(this.Writer);
				}
			}
			// wykonuje watki
			foreach (var thread in threads) thread.Start();
			foreach (var thread in threads) thread.Join();
			return new int[2] { numberOfReaders, numberOfWriters };
		}
		// wykonuje losowo operacje czytania i pisania
		public void PerformOperation(int numberOfThreads) // ilość wątkow - max ilosć wątków, które mogą być zrównoleglone
		{
			int quantityToDo = NumberOfReaders + NumberOfWriters;
			int quantityOfRepetionsOfLoop = quantityToDo / numberOfThreads; // ilość powtarzania pętli by była wykorzystana maxymalna ilość wątków
			int[] quantity = { NumberOfReaders, NumberOfWriters };
			while (quantityOfRepetionsOfLoop > 0)
			{
				quantity = DoThreadingWork(numberOfThreads, quantity[0], quantity[1]);
				quantityOfRepetionsOfLoop--;
			}
			// wykonuje funkcje dla pozostalej ilosci czytaczy i pisaczy równolegle
			DoThreadingWork(quantity[0] + quantity[1], quantity[0], quantity[1]);
		}
		public int DrawingALock() // losuje zamek dla danego wątka
		{
			Random rnd = new Random();
			return rnd.Next(0, NumberOfLocks);
		}
		public void Writer()
		{
			while (NumberOfAvailableLockers != NumberOfLocks) ;
			ChangeNumberOfAvailableLockers.Wait();
			int lockIndex = DrawingALock();
			TableOfLocks[lockIndex].WrLock();
			Thread.Sleep(rng.Next(500, 1000)); // writer wykonuje swoje działanie
			TableOfLocks[lockIndex].WrUnlock();
			ChangeNumberOfAvailableLockers.Release();
		}
		public void Reader()
		{
			// aby żaden wątek w tym samym czasie nie zmieniał wartości globalnej
			// IlośćDostępnychZamków korzystam z semafora
			ChangeNumberOfAvailableLockers.Wait();
			NumberOfAvailableLockers--;
			ChangeNumberOfAvailableLockers.Release();

			int lockIndex = DrawingALock();
			TableOfLocks[lockIndex].RdLock();
			Thread.Sleep(rng.Next(500, 1000)); // reader czyta
			TableOfLocks[lockIndex].RdUnlock();

			ChangeNumberOfAvailableLockers.Wait();
			NumberOfAvailableLockers++;
			ChangeNumberOfAvailableLockers.Release();
		}
	}
	class RWLock
	{
		int state;
		bool hasWriterWaiting = false;
		Object cond = new Object();
		Random rng = new Random();

		public RWLock()
		{
			state = 0;
		}

		public bool IsLocked()
		{
			return state != 0;
		}
		public void RdLock()
		{
			lock (cond)
			{
				while (state == -1 || hasWriterWaiting) Monitor.Wait(cond);
				++state;
				Console.WriteLine("Reader Lock");
			}
		}

		public void WrLock()
		{
			lock (cond)
			{
				if (state != 0)
					hasWriterWaiting = true;
				while (state != 0) Monitor.Wait(cond);
				state = -1;
				Console.WriteLine("Writer Lock");
				hasWriterWaiting = false;
			}
		}

		public void RdUnlock()
		{
			lock (cond)
			{
				if (state <= 0) throw new InvalidUnlockOperation();
				if (--state == 0) Monitor.Pulse(cond);
				Console.WriteLine("Reader UnLock");
			}
		}

		public void WrUnlock()
		{
			lock (cond)
			{
				if (state != -1) throw new InvalidUnlockOperation();
				state = 0;
				Monitor.PulseAll(cond);
				Console.WriteLine("Writer UnLock");
			}
		}
	}
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Example  1");
			RWLock[] lck = new RWLock[2];
			BigReaderLock b = new BigReaderLock(lck, 5, 2);
			b.PerformOperation(3);

			b = new BigReaderLock(lck, 20, 2);
			Console.WriteLine("\n\nExample  2");
			b.PerformOperation(5);

			lck = new RWLock[4];
			b = new BigReaderLock(lck, 40, 7);
			Console.WriteLine("\n\n Example  3");
			b.PerformOperation(8);


			Console.ReadKey();
		}
	}
}
