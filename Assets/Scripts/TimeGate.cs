using UnityEngine;

namespace Utilities.Timers
{
    /// <summary>
    /// Một utility linh hoạt để giới hạn tần suất của một hành động.
    /// Nó không cần chạy trong Update() và hoạt động dựa trên logic timestamp.
    /// </summary>
    public sealed class TimeGate
    {
        private float _interval;
        private float _lastPassTime;

        /// <summary>
        /// Thời gian (giây) cần chờ giữa mỗi lần cổng được đi qua.
        /// Có thể thay đổi giá trị này bất kỳ lúc nào.
        /// </summary>
        public float Interval
        {
            get => _interval;
            set => _interval = Mathf.Max(0f, value); // Đảm bảo interval không bao giờ âm
        }

        /// <summary>
        /// Trả về true nếu cổng đã sẵn sàng để đi qua.
        /// Việc kiểm tra này không làm thay đổi trạng thái của cổng.
        /// </summary>
        public bool IsReady => Time.time >= _lastPassTime + _interval;

        /// <summary>
        /// Thời gian còn lại (giây) cho đến khi cổng sẵn sàng.
        /// Trả về 0 nếu cổng đã sẵn sàng.
        /// </summary>
        public float RemainingTime => Mathf.Max(0f, (_lastPassTime + _interval) - Time.time);

        /// <summary>
        /// Tiến trình cooldown hiện tại, từ 0.0 (vừa bắt đầu) đến 1.0 (đã sẵn sàng).
        /// </summary>
        public float Progress
        {
            get
            {
                if (_interval <= 0f || IsReady)
                {
                    return 1f;
                }
                float elapsedTime = Time.time - _lastPassTime;
                return Mathf.Clamp01(elapsedTime / _interval);
            }
        }

        /// <summary>
        /// Khởi tạo một TimeGate với khoảng thời gian chờ.
        /// </summary>
        /// <param name="initialInterval">Thời gian (giây) ban đầu cần chờ.</param>
        public TimeGate(float initialInterval)
        {
            Interval = initialInterval;
            // Đặt thời gian lần cuối đi qua là một giá trị âm
            // để đảm bảo lần kiểm tra đầu tiên luôn thành công.
            _lastPassTime = -_interval;
        }

        /// <summary>
        /// Kiểm tra xem cổng đã sẵn sàng để đi qua chưa.
        /// Nếu có, nó sẽ tự động bắt đầu cooldown và trả về true.
        /// </summary>
        /// <returns>True nếu đã đủ thời gian chờ, ngược lại là false.</returns>
        public bool TryPass()
        {
            if (!IsReady)
            {
                return false;
            }

            _lastPassTime = Time.time;
            return true;
        }

        /// <summary>
        /// Buộc cổng bắt đầu chu kỳ cooldown ngay lập tức từ thời điểm hiện tại.
        /// </summary>
        public void StartCooldown()
        {
            _lastPassTime = Time.time;
        }

        /// <summary>
        /// Đặt lại cổng về trạng thái sẵn sàng ngay lập tức.
        /// Lần gọi TryPass() tiếp theo sẽ luôn thành công.
        /// </summary>
        public void Reset()
        {
            _lastPassTime = Time.time - _interval;
        }
    }
}