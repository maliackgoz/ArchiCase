export default function Button({ children, onClick, variant = 'primary', disabled, type = 'button', small }) {
  return (
    <button
      type={type}
      onClick={onClick}
      disabled={disabled}
      className={`btn btn-${variant}${small ? ' btn-small' : ''}`}
    >
      {children}
    </button>
  )
}
