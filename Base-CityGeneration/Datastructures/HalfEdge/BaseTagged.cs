namespace Base_CityGeneration.Datastructures.HalfEdge
{
    /// <summary>
    /// A thing with a tag
    /// </summary>
    /// <typeparam name="TTag">The type of the tag</typeparam>
    /// <typeparam name="TAttachable">The type which the tag *might* implement, if it does then Attach and Detach will be called</typeparam>
    /// <typeparam name="TContainer">The container type (i.e. the type of whatever extends this class)</typeparam>
    public class BaseTagged<TTag, TAttachable, TContainer>
        where TAttachable : class, IAttachable<TContainer>
        where TContainer : BaseTagged<TTag, TAttachable, TContainer>
    {
        private TTag _tag;

        /// <summary>
        /// The tag attached to this vertex (may be null).
        /// If this tag implements IVertexTag then Attach and Detach will be called appropriately
        /// </summary>
        public TTag Tag
        {
            get { return _tag; }
            set
            {
                SetTag(value, true, true);
            }
        }

        /// <summary>
        /// Set the vertex tag, potentially without unsetting the old one
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="attach">Indicates if Attach will be called on new tag</param>
        /// <param name="detach">Indicates if detach will be called on old tag</param>
        /// <remarks>Technically using this with [unset=false] breaks the IAttachable contract, use with caution - With great power comes great responsibilty!</remarks>
        internal void SetTag(TTag tag, bool attach, bool detach)
        {
            //Do nothing if the tag has not actually changed
            if ((_tag == null && tag == null) || (_tag != null &&  _tag.Equals(tag)))
                return;

            //Detach old tag (if possible)
            var oldTag = _tag as TAttachable;
            if (oldTag != null && detach)
                oldTag.Detach((TContainer)this);

            //Attach new tag (if possible)
            var newTag = tag as TAttachable;
            if (newTag != null && attach)
                newTag.Attach((TContainer)this);

            _tag = tag;
        }
    }
}
