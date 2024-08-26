namespace FusionProgram.Locked_Example
{
    public class LockExtension
    {
        #region 自旋锁
        private static int _SpinLock = 0;//锁对象
        private static int incrValue = 0;//共享资源
        private void Run()
        {
            //获取锁
            //使用int原子操作将_SpinLock的值赋值为1，Interlocked.Exchange(ref _SpinLock, 1)的返回值为改变之前的值。
            //如果返回0，则获取到了锁， 如果返回1，则锁被占用
            while (Interlocked.Exchange(ref _SpinLock, 1) != 0)
            {
                Thread.SpinWait(1);//自旋锁等待
            }

            incrValue++;  //安全的逻辑计算

            //释放锁：将_SpinLock重置会0；
            Interlocked.Exchange(ref _SpinLock, 0);
        }
        #endregion

        #region 互斥锁
        private static readonly Mutex _mutexLock = new Mutex();
        private void Run2()
        {
            _mutexLock.WaitOne();//获取锁, 
            try
            {
                incrValue++;  //安全的逻辑计算
            }
            finally
            {
                _mutexLock.ReleaseMutex();//释放锁
            }
        }
        #endregion

        #region 混合锁
        private static readonly object _monitorLock = new object();
        private void Run3()
        {
            var islocked = false;
            try
            {
                Monitor.Enter(_monitorLock, ref islocked);  //获取锁,获取成功islocked被置为true,获取失败则会阻塞，直到该锁被释放

                incrValue++;  //安全的逻辑计算
            }
            finally
            {
                if (islocked)
                {
                    Monitor.Exit(_monitorLock);// 释放锁
                }
            }
        }
        #endregion
    }
}
