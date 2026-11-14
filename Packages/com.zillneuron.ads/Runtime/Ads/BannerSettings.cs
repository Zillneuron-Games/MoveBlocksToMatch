using System;

namespace Zillneuron.Ads
{
    public class BannerSettings
    {
        private int width;
        private int height;
        private EAdPosition position;

        public int Width => width;
        public int Height => height;
        public EAdPosition Position => position;

        public BannerSettings(int width, int height, EAdPosition position)
        {
            this.width = width;
            this.height = height;
            this.position = position;
        }
    }
}