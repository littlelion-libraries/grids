#define USE_VECTOR_3_GRID_AGENT
using System;
using System.Numerics;
using Numerics;
using Times;

namespace Grids
{
#if USE_DOUBLE_3_GRID_AGENT
    using GridAgentVector3 = Double3;
#endif
#if USE_VECTOR_3_GRID_AGENT
    using GridAgentVector3 = Vector3;
#endif

    public class GridAgent : IGridAgent
    {
        private enum Axis
        {
            X,
            Y,
            Z
        }

        private Axis _axis;
        private int _direction;
        private Int3 _forward;
        private Func<Vector3, Int3> _inverseTransform;
        private Func<Int3, bool> _isWalkable;
        private float _speed;
        private Func<Int3, Vector3> _transform;

        public Int3 Forward
        {
            set
            {
                if (Math.Abs(value.X) + Math.Abs(value.Y) + Math.Abs(value.Z) != 1)
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
                _forward = value;
                switch (value.X)
                {
                    case > 1:
                        _axis = Axis.X;
                        _direction = 1;
                        return;
                    case < 1:
                        _axis = Axis.X;
                        _direction = -1;
                        return;
                }

                switch (value.Y)
                {
                    case > 1:
                        _axis = Axis.Y;
                        _direction = 1;
                        return;
                    case < 1:
                        _axis = Axis.Y;
                        _direction = -1;
                        return;
                }

                switch (value.Z)
                {
                    case > 1:
                        _axis = Axis.Z;
                        _direction = 1;
                        return;
                    case < 1:
                        _axis = Axis.Z;
                        _direction = -1;
                        return;
                }

                throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        public Func<Vector3, Int3> InverseTransform
        {
            set => _inverseTransform = value;
        }

        public Func<Int3, bool> IsWalkable
        {
            get => _isWalkable;
            set => _isWalkable = value;
        }

        public float Speed
        {
            set => _speed = value;
        }

        public Func<Int3, Vector3> Transform
        {
            set => _transform = value;
        }

        public bool CanMove(Vector3 position)
        {
            var inversePosition = _inverseTransform(position);
            if (IsWalkable(inversePosition + _forward)) return true;
            var centerDelta = _transform(inversePosition) - position;
            return centerDelta.LengthSquared() > 0;
        }

        private static bool ChangeDirectionMoveToCenter(
            float center,
            float delta,
            int direction,
            ref float position,
            ref float subPosition
        )
        {
            var centerDelta = center - position;
            var absCenterDelta = Math.Abs(delta);
            if (absCenterDelta < float.Epsilon) return false;
            var lesser = absCenterDelta < delta;
            if (lesser) subPosition += (delta - absCenterDelta) * direction;
            position = lesser ? center : delta * centerDelta.Normalize();
            return true;
        }

        private static bool ChangeDirectionMoveToCenter(
            Axis axis,
            Vector3 center,
            float delta,
            int direction,
            ref GridAgentVector3 position
        )
        {
            return axis switch
            {
                Axis.X => ChangeDirectionMoveToCenter(center.Z, delta, direction, ref position.Z, ref position.X),
                Axis.Y => throw new ArgumentOutOfRangeException(nameof(axis), axis, null),
                Axis.Z => ChangeDirectionMoveToCenter(center.X, delta, direction, ref position.X, ref position.Z),
                _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
            };
        }
#if USE_DOUBLE_3_GRID_AGENT
        private static double GetDelta(TimeSpan dt)
        {
            return dt.TotalSeconds;
        }
#endif
#if USE_VECTOR_3_GRID_AGENT
        private static float GetDelta(ShortTimeSpan dt)
        {
            return dt.Seconds;
        }
#endif

        public Int3 GetNextCell(Int3 direction, Vector3 position)
        {
            return _inverseTransform(position) + direction;
        }

        public bool MoveToCenterStep(ShortTimeSpan dt, ref GridAgentVector3 position)
        {
            if (_speed == 0) return false;
            var inversePosition = _inverseTransform(position);
            var center = _transform(inversePosition);
            var neighbor = inversePosition + _forward;
            var delta = _speed * dt;
            if (!IsWalkable(neighbor)) return NotWalkableMoveToCenter(_axis, center, delta, ref position);
            if (ChangeDirectionMoveToCenter(_axis, center, delta, _direction, ref position)) return true;
            position += delta * _forward;
            return true;
        }

        private static bool NotWalkableMoveToCenter(float center, float delta, ref float position)
        {
            var centerVector = center - position;
            var centerDistance = Math.Abs(centerVector);
            if (centerDistance < float.Epsilon) return false;
            position = centerDistance < delta ? center : position + delta * centerVector.Normalize();
            return true;
        }

        private static bool NotWalkableMoveToCenter(Axis axis, Vector3 center, float delta, ref GridAgentVector3 position)
        {
            return axis switch
            {
                Axis.X => NotWalkableMoveToCenter(center.X, delta, ref position.X),
                Axis.Y => throw new ArgumentOutOfRangeException(nameof(axis), axis, null),
                Axis.Z => NotWalkableMoveToCenter(center.Z, delta, ref position.Z),
                _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
            };
        }
#if USE_DOUBLE_3_GRID_AGENT
        private static double SqrMagnitude(GridAgentVector3 value)
        {
            return value.SqrMagnitude();
        }
#endif
#if USE_VECTOR_3_GRID_AGENT
        private static double SqrMagnitude(GridAgentVector3 value)
        {
            return value.LengthSquared();
        }
#endif
        public bool Step(ShortTimeSpan dt, ref GridAgentVector3 position)
        {
            var inversePosition = _inverseTransform(position);
            var delta = _forward * _speed * GetDelta(dt);
            if (IsWalkable(inversePosition + _forward))
            {
                position += delta;
                return true;
            }

            var centerPosition = _transform(inversePosition);
            var centerDelta = centerPosition - position;
            var sqrCenterDelta = SqrMagnitude(centerDelta);
            if (sqrCenterDelta == 0) return false;
            position = SqrMagnitude(centerDelta) > SqrMagnitude(delta) ? position + delta : centerPosition;
            return true;
        }
    }
}