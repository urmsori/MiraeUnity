using Mirae.Network;
using System.Collections;
using System.Collections.Generic;

namespace Mirae.CodeBlockEngine
{
    public class MiraeBlockCodeEngine
    {
        private MiraeInterpreter mInterpreter = null;
        private MiraeCodeRunner mRunner = null;
        private MiraeBuildResult mBuildResult;

        public MiraeEnvironment Environment
        {
            set
            {
                mInterpreter = new MiraeInterpreter(value);
            }
        }

        public MiraeBlockCodeEngine(MiraeEnvironment environment)
        {
            Environment = environment;
        }

        public MiraeBuildResult Build(IEnumerable<MiraeNetworkBlock> networkBlocks)
        {
            mBuildResult = mInterpreter.Build(networkBlocks);
            if (mBuildResult.Result == MiraeBuildError.Success)
            {
                mRunner = new MiraeCodeRunner(mBuildResult.BlockCodes, mBuildResult.StartingBlockName);
            }
            return mBuildResult;
        }

        public MiraeRuntimeResult RunOnce()
        {
            if (mRunner == null)
                return new MiraeRuntimeResult(MiraeRuntimeResultType.Error, MiraeRuntimeError.NotBuild, null);
            return mRunner.Next();
        }

        
    }
}
