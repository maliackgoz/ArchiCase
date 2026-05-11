export default function ErrorBanner({ message, onDismiss }) {
  if (!message) return null
  return (
    <div className="error-banner" role="alert">
      <span>{message}</span>
      {onDismiss && <button onClick={onDismiss} className="error-dismiss">×</button>}
    </div>
  )
}
